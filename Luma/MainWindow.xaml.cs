using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Luma.Imaging;
using Luma.Services;
using Luma.Ocr;
using Microsoft.Win32;

namespace Luma;

public partial class MainWindow : Window
{
    private readonly ImageLoader loader = new();
    private readonly ThemeService themes = new();
    private readonly OcrService ocr = new();
    private CancellationTokenSource? ocrCancellation;
    private bool selectingText;
    private string? ocrStatus;
    private HwndSource? windowSource;
    private readonly DispatcherTimer animation = new(DispatcherPriority.Render);
    private CancellationTokenSource? loadCancellation;
    private ImageDocument? document;
    private string? currentPath;
    private string[] folder = [];
    private int index = -1, frameIndex;
    private uint completedLoops;
    private bool fit = true, fullscreen, closed;
    private double zoom = 1;
    private WindowState previousState;
    private Rect previousBounds;
    private Point? panOrigin;
    private double panX, panY;

    public MainWindow(string? initialPath)
    {
        InitializeComponent();
        UpdateThemeMenu();
        L.Changed += LanguageChanged;
        animation.Tick += AnimationTick;
        Loaded += async (_, _) => { if (!string.IsNullOrWhiteSpace(initialPath)) await OpenAsync(initialPath); };
        Closed += (_, _) => { closed = true; L.Changed -= LanguageChanged; ocrCancellation?.Cancel(); windowSource?.RemoveHook(WindowMessage); animation.Stop(); loadCancellation?.Cancel(); document = null; Picture.Source = null; };
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized) animation.Stop();
            else if (document?.Animated == true && (document.Iterations == 0 || completedLoops < document.Iterations)) animation.Start();
        };
    }

    private async Task OpenAsync(string path, bool rescan = true)
    {
        loadCancellation?.Cancel();
        ocrCancellation?.Cancel();
        TextOverlay.SetText(null); ocrStatus = null;
        var cancellation = new CancellationTokenSource();
        loadCancellation = cancellation;
        var token = cancellation.Token;
        animation.Stop(); document = null; Picture.Source = null;
        if (rescan) { folder = []; index = -1; }
        Message.Text = L.Text("Loading"); Message.Visibility = Visibility.Visible;
        PreviousButton.IsEnabled = NextButton.IsEnabled = false;
        try
        {
            path = Path.GetFullPath(path);
            if (!FolderNavigator.Supported(path)) throw new InvalidDataException(L.Text("ChooseImage"));
            var loaded = await loader.LoadAsync(path, token);
            if (closed || token.IsCancellationRequested) return;
            document = loaded; currentPath = path;
            frameIndex = 0; completedLoops = 0; Rotation.Angle = 0; fit = true;
            Picture.Source = loaded.Frames[0].Bitmap;
            Message.Visibility = Visibility.Collapsed;
            Title = $"{Path.GetFileName(path)} — Luma";
            UpdateScale();
            if (!loaded.Animated) QueueRecognition(loaded);
            if (loaded.Animated) { animation.Interval = loaded.Frames[0].Delay; animation.Start(); }
            if (rescan)
            {
                try
                {
                    var scanned = await Task.Run(() => FolderNavigator.Scan(path, token), token);
                    if (closed || token.IsCancellationRequested) return;
                    folder = scanned;
                }
                catch (UnauthorizedAccessException) { Status.Text += " · " + L.Text("FolderDenied"); }
                catch (IOException) { Status.Text += " · " + L.Text("FolderUnavailable"); }
            }
            if (closed || token.IsCancellationRequested) return;
            index = Array.FindIndex(folder, file => string.Equals(file, path, StringComparison.OrdinalIgnoreCase));
            UpdateNavigation();
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!closed && !token.IsCancellationRequested)
            {
                currentPath = null; Message.Text = L.Text("UnableOpen") + "\n" + FriendlyError(error);
                Status.Text = L.Text("OpenAnother");
                if (!rescan) UpdateNavigation();
            }
        }
        finally
        {
            if (ReferenceEquals(loadCancellation, cancellation)) loadCancellation = null;
            cancellation.Dispose();
        }
    }

    private void AnimationTick(object? sender, EventArgs e)
    {
        if (document?.Animated != true) return;
        if (++frameIndex >= document.Frames.Count)
        {
            completedLoops++;
            if (document.Iterations != 0 && completedLoops >= document.Iterations) { animation.Stop(); return; }
            frameIndex = 0;
        }
        var frame = document.Frames[frameIndex];
        Picture.Source = frame.Bitmap; animation.Interval = frame.Delay;
    }

    private void UpdateScale()
    {
        if (document == null) return;
        var bitmap = document.Frames[0].Bitmap;
        bool sideways = Rotation.Angle % 180 != 0;
        double width = sideways ? bitmap.PixelHeight : bitmap.PixelWidth;
        double height = sideways ? bitmap.PixelWidth : bitmap.PixelHeight;
        // 100% maps one decoded pixel to one physical screen pixel, including HiDPI.
        double dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this).DpiScaleX;
        double scale = zoom / dpi;
        if (fit)
        {
            scale = Math.Min(1 / dpi, Math.Min(Math.Max(1, Viewport.ActualWidth - 24) / width, Math.Max(1, Viewport.ActualHeight - 24) / height));
            zoom = scale * dpi;
        }
        Picture.Width = bitmap.PixelWidth * scale; Picture.Height = bitmap.PixelHeight * scale;
        ImageSurface.Width = Picture.Width; ImageSurface.Height = Picture.Height;
        Status.Text = $"{Path.GetFileName(currentPath)} · {bitmap.PixelWidth} × {bitmap.PixelHeight} · {zoom:P0}" +
            (document.Animated ? " · " + L.Format("Frames", document.Frames.Count) : "") +
            (ocrStatus == null ? "" : " · " + L.Text(ocrStatus));
    }

    private void UpdateNavigation()
    {
        PreviousButton.IsEnabled = index > 0; NextButton.IsEnabled = index >= 0 && index + 1 < folder.Length;
    }
    private async Task NavigateAsync(int direction)
    {
        int target = index + direction;
        if (target < 0 || target >= folder.Length) return;
        index = target; await OpenAsync(folder[target], false);
    }
    private void ChangeZoom(double factor) { if (document == null) return; fit = false; zoom = Math.Clamp(zoom * factor, 0.01, 16); UpdateScale(); }
    private void Fit() { fit = true; UpdateScale(); Viewport.ScrollToHorizontalOffset(0); Viewport.ScrollToVerticalOffset(0); }
    private void Rotate() { Rotation.Angle = (Rotation.Angle + 90) % 360; UpdateScale(); }
    private void ToggleFullscreen()
    {
        if (!fullscreen)
        {
            previousState = WindowState;
            previousBounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            Toolbar.Visibility = StatusBar.Visibility = Visibility.Collapsed;
            WindowState = WindowState.Maximized; fullscreen = true;
        }
        else
        {
            WindowState = WindowState.Normal; WindowStyle = WindowStyle.SingleBorderWindow; ResizeMode = ResizeMode.CanResize;
            Left = previousBounds.Left; Top = previousBounds.Top; Width = previousBounds.Width; Height = previousBounds.Height;
            Toolbar.Visibility = StatusBar.Visibility = Visibility.Visible;
            WindowState = previousState; fullscreen = false;
        }
        UpdateScale();
    }
    private async void OpenClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = L.Text("ImagesFilter") + "|" + FolderNavigator.DialogFilter, CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) await OpenAsync(dialog.FileName);
    }
    private async void PreviousClick(object sender, RoutedEventArgs e) => await NavigateAsync(-1);
    private async void NextClick(object sender, RoutedEventArgs e) => await NavigateAsync(1);
    private void ZoomOutClick(object sender, RoutedEventArgs e) => ChangeZoom(1 / 1.25);
    private void ZoomInClick(object sender, RoutedEventArgs e) => ChangeZoom(1.25);
    private void FitClick(object sender, RoutedEventArgs e) => Fit();
    private void ActualClick(object sender, RoutedEventArgs e) { fit = false; zoom = 1; UpdateScale(); }
    private void RotateClick(object sender, RoutedEventArgs e) => Rotate();
    private void FullscreenClick(object sender, RoutedEventArgs e) => ToggleFullscreen();
    private void ViewportChanged(object sender, SizeChangedEventArgs e) => UpdateScale();
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) { base.OnDpiChanged(oldDpi, newDpi); UpdateScale(); }
    private void OnWheel(object sender, MouseWheelEventArgs e) { ChangeZoom(e.Delta > 0 ? 1.25 : 1 / 1.25); e.Handled = true; }
    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files) await OpenAsync(files[0]);
    }
    private async void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.C when Keyboard.Modifiers == ModifierKeys.Control: CopyText(); break;
            case Key.O when Keyboard.Modifiers == ModifierKeys.Control: OpenClick(sender, e); break;
            case Key.Left: await NavigateAsync(-1); break;
            case Key.Right: await NavigateAsync(1); break;
            case Key.Add: case Key.OemPlus: ChangeZoom(1.25); break;
            case Key.Subtract: case Key.OemMinus: ChangeZoom(1 / 1.25); break;
            case Key.F: Fit(); break;
            case Key.D1: case Key.NumPad1: fit = false; zoom = 1; UpdateScale(); break;
            case Key.R: Rotate(); break;
            case Key.F11: ToggleFullscreen(); break;
            case Key.Escape when fullscreen: ToggleFullscreen(); break;
            default: return;
        }
        e.Handled = true;
    }
    private void PanStart(object sender, MouseButtonEventArgs e)
    {
        if (document == null || (e.OriginalSource is not System.Windows.Controls.Image && e.OriginalSource is not TextSelectionOverlay)) return;
        int word = TextOverlay.HitWord(e.GetPosition(TextOverlay));
        if (word >= 0 && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            selectingText = true; TextOverlay.SelectFrom(word);
            Viewport.CaptureMouse(); Viewport.Cursor = Cursors.IBeam; e.Handled = true; return;
        }
        TextOverlay.ClearSelection();
        panOrigin = e.GetPosition(Viewport); panX = Viewport.HorizontalOffset; panY = Viewport.VerticalOffset;
        Viewport.CaptureMouse(); Viewport.Cursor = Cursors.Hand; e.Handled = true;
    }
    private void PanMove(object sender, MouseEventArgs e)
    {
        if (selectingText) { TextOverlay.ExtendTo(e.GetPosition(TextOverlay)); return; }
        if (panOrigin is not Point start) return;
        var point = e.GetPosition(Viewport);
        Viewport.ScrollToHorizontalOffset(panX + start.X - point.X);
        Viewport.ScrollToVerticalOffset(panY + start.Y - point.Y);
    }
    private void PanEnd(object sender, MouseButtonEventArgs e) { selectingText = false; panOrigin = null; Viewport.ReleaseMouseCapture(); Viewport.Cursor = null; }
    private void PanLost(object sender, MouseEventArgs e) { selectingText = false; panOrigin = null; Viewport.Cursor = null; }
    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        windowSource = HwndSource.FromHwnd(handle);
        windowSource?.AddHook(WindowMessage);
        ApplyTitleTheme();
        WindowAppearance.ApplyCorners(handle);
    }
    private void ThemeClick(object sender, RoutedEventArgs e)
    {
        ThemeButton.ContextMenu.PlacementTarget = ThemeButton;
        ThemeButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        ThemeButton.ContextMenu.IsOpen = true;
    }
    private void ThemeSelected(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && item.Tag is string value && Enum.TryParse<AppTheme>(value, out var choice))
        {
            themes.Select(choice); UpdateThemeMenu(); ApplyTitleTheme();
        }
    }
    private void UpdateThemeMenu()
    {
        LightThemeItem.IsChecked = themes.Preference == AppTheme.Light;
        DarkThemeItem.IsChecked = themes.Preference == AppTheme.Dark;
        SystemThemeItem.IsChecked = themes.Preference == AppTheme.System;
    }
    private void ApplyTitleTheme()
    {
        WindowAppearance.ApplyDarkTitle(new WindowInteropHelper(this).Handle, themes.IsDark);
    }
    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x001A) Dispatcher.BeginInvoke(() => { if (!closed) L.Refresh(); });
        if ((message == 0x001A || message == 0x031A) && themes.Preference == AppTheme.System)
            Dispatcher.BeginInvoke(() => { if (!closed) { themes.Refresh(); ApplyTitleTheme(); } });
        return IntPtr.Zero;
    }
    private void AboutClick(object sender, RoutedEventArgs e) => new AboutWindow(themes.IsDark) { Owner = this }.ShowDialog();
    private void TextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
    {
        if (!TextOverlay.HasText) { e.Handled = true; return; }
        CopyTextItem.IsEnabled = TextOverlay.HasSelection;
    }
    private void CopyTextClick(object sender, RoutedEventArgs e) => CopyText();
    private void CopyAllTextClick(object sender, RoutedEventArgs e) => CopyText(true);
    private void CopyText(bool all = false)
    {
        string value = TextOverlay.SelectedText(all);
        if (value.Length == 0) return;
        try { Clipboard.SetText(value); }
        catch (ExternalException) { Status.Text = L.Text("CopyFailed"); }
    }
    private void QueueRecognition(ImageDocument image)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
            if (!closed && ReferenceEquals(document, image)) _ = RecognizeTextAsync(image);
        }));
    }
    private async Task RecognizeTextAsync(ImageDocument image)
    {
        ocrCancellation?.Cancel();
        var cancellation = new CancellationTokenSource(); ocrCancellation = cancellation;
        ocrStatus = "TextLoading"; UpdateScale();
        try
        {
            var recognized = await ocr.RecognizeAsync(image.Frames[0].Bitmap, L.Language, cancellation.Token);
            if (closed || cancellation.IsCancellationRequested || !ReferenceEquals(document, image)) return;
            TextOverlay.SetText(recognized); ocrStatus = recognized.Words.Count > 0 ? "TextAvailable" : "NoText";
            UpdateScale();
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!closed && !cancellation.IsCancellationRequested && ReferenceEquals(document, image)) { ocrStatus = "TextUnavailable"; UpdateScale(); }
        }
        finally { if (ReferenceEquals(ocrCancellation, cancellation)) ocrCancellation = null; cancellation.Dispose(); }
    }
    private void LanguageChanged()
    {
        if (closed) return;
        UpdateScale();
        if (document == null) { Message.Text = L.Text("Empty"); Status.Text = L.Text("Description"); }
        else if (!document.Animated) { ocrCancellation?.Cancel(); TextOverlay.SetText(null); QueueRecognition(document); }
    }
    private static string FriendlyError(Exception error) => error switch {
        FileNotFoundException or DirectoryNotFoundException => L.Text("FileMissing"),
        UnauthorizedAccessException => L.Text("AccessDenied"),
        InvalidDataException => error.Message,
        IOException => L.Text("ReadError"),
        _ => L.Text("CorruptFormat") };
}
