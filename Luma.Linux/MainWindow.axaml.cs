using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using System.ComponentModel;
using System.Reflection;
using Luma.Imaging;
using Luma.Services;
using Luma.Ocr;

namespace Luma;

public partial class MainWindow : Window
{
    private readonly ImageLoader loader = new();
    private readonly ThemeService themes = new();
    private readonly OcrService ocr = new();
    private CancellationTokenSource? ocrCancellation;
    private bool selectingText;
    private string? ocrStatus;
    private readonly DispatcherTimer animation = new();
    private RotateTransform Rotation => (RotateTransform)TransformSurface.LayoutTransform!;
    private CancellationTokenSource? loadCancellation;
    private ImageDocument? document;
    private string? currentPath;
    private string[] folder = [];
    private int index = -1, frameIndex;
    private uint completedLoops;
    private bool fit = true, fullscreen, closed;
    private double zoom = 1;
    private WindowState previousState;
    
    private Point? panOrigin;
    private double panX, panY;

    public MainWindow() : this(null) { }
    public MainWindow(string? initialPath)
    {
        InitializeComponent();
        using (var icon=Assembly.GetExecutingAssembly().GetManifestResourceStream("Luma.Icon.png")) Icon=new WindowIcon(icon!);
        DragDrop.SetAllowDrop(this,true); AddHandler(DragDrop.DropEvent,OnDrop);
        AddHandler(KeyDownEvent,OnKeyDown,RoutingStrategies.Tunnel);
        Viewport.AddHandler(PointerWheelChangedEvent,OnWheel,RoutingStrategies.Tunnel);
        Viewport.AddHandler(PointerPressedEvent,PanStart,RoutingStrategies.Tunnel);
        Viewport.AddHandler(PointerReleasedEvent,PanEnd,RoutingStrategies.Tunnel);
        Viewport.PointerMoved+=PanMove; Viewport.PointerCaptureLost+=PanLost;
        Viewport.SizeChanged+=ViewportChanged;
        ScalingChanged+=(_,_)=>UpdateScale();
        Activated+=(_,_)=>L.Refresh();
        ActualThemeVariantChanged+=(_,_)=>themes.RefreshColors();
        TextMenu.Opening+=(_,e)=>{CopyTextItem.IsEnabled=TextOverlay.HasSelection;if(!TextOverlay.HasText)e.Cancel=true;};
        UpdateThemeMenu();
        L.Changed += LanguageChanged;
        animation.Tick += AnimationTick;
        Opened += async (_, _) => { if (!string.IsNullOrWhiteSpace(initialPath)) await OpenAsync(initialPath); };
        Closed += (_, _) => { closed = true; L.Changed -= LanguageChanged; ocrCancellation?.Cancel(); animation.Stop(); loadCancellation?.Cancel(); Picture.Source = null; document?.Dispose(); document = null; };
        PropertyChanged += (_, change) =>
        {
            if (change.Property != WindowStateProperty) return;
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
        animation.Stop(); Picture.Source = null; document?.Dispose(); document = null;
        if (rescan) { folder = []; index = -1; }
        Message.Text = L.Text("Loading"); Message.IsVisible = true;
        PreviousButton.IsEnabled = NextButton.IsEnabled = false;
        try
        {
            path = Path.GetFullPath(path);
            if (!FolderNavigator.Supported(path)) throw new InvalidDataException(L.Text("ChooseImage"));
            var loaded = await loader.LoadAsync(path, token);
            if (closed || token.IsCancellationRequested) { loaded.Dispose(); return; }
            document = loaded; currentPath = path;
            frameIndex = 0; completedLoops = 0; Rotation.Angle = 0; fit = true;
            Picture.Source = loaded.Frames[0].Bitmap;
            Message.IsVisible = false;
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
            index = Array.FindIndex(folder, file => string.Equals(file, path, StringComparison.Ordinal));
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
        double width = sideways ? bitmap.PixelSize.Height : bitmap.PixelSize.Width;
        double height = sideways ? bitmap.PixelSize.Width : bitmap.PixelSize.Height;
        // 100% maps one decoded pixel to one physical screen pixel, including HiDPI.
        double dpi = RenderScaling;
        double scale = zoom / dpi;
        if (fit)
        {
            scale = Math.Min(1 / dpi, Math.Min(Math.Max(1, Viewport.Bounds.Width - 24) / width, Math.Max(1, Viewport.Bounds.Height - 24) / height));
            zoom = scale * dpi;
        }
        Picture.Width = bitmap.PixelSize.Width * scale; Picture.Height = bitmap.PixelSize.Height * scale;
        ImageSurface.Width = Picture.Width; ImageSurface.Height = Picture.Height;
        Status.Text = $"{Path.GetFileName(currentPath)} · {bitmap.PixelSize.Width} × {bitmap.PixelSize.Height} · {zoom:P0}" +
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
    private void Fit() { fit = true; UpdateScale(); Viewport.Offset=default; }
    private void Rotate() { Rotation.Angle = (Rotation.Angle + 90) % 360; UpdateScale(); }
    private void ToggleFullscreen()
    {
        if (!fullscreen) { previousState=WindowState;Toolbar.IsVisible=StatusBar.IsVisible=false;WindowState=WindowState.FullScreen;fullscreen=true; }
        else { WindowState=previousState;Toolbar.IsVisible=StatusBar.IsVisible=true;fullscreen=false; }
        UpdateScale();
    }
    private async void OpenClick(object? sender,RoutedEventArgs e)
    {
        var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title=L.Text("Open"),AllowMultiple=false,FileTypeFilter=[new FilePickerFileType(L.Text("ImagesFilter")){Patterns=FolderNavigator.Extensions.Select(x=>"*"+x).ToArray()}] });
        if(files.FirstOrDefault()?.TryGetLocalPath() is string path) await OpenAsync(path);
    }
    private async void PreviousClick(object? sender, RoutedEventArgs e) => await NavigateAsync(-1);
    private async void NextClick(object? sender, RoutedEventArgs e) => await NavigateAsync(1);
    private void ZoomOutClick(object? sender, RoutedEventArgs e) => ChangeZoom(1 / 1.25);
    private void ZoomInClick(object? sender, RoutedEventArgs e) => ChangeZoom(1.25);
    private void FitClick(object? sender, RoutedEventArgs e) => Fit();
    private void ActualClick(object? sender, RoutedEventArgs e) { fit = false; zoom = 1; UpdateScale(); }
    private void RotateClick(object? sender, RoutedEventArgs e) => Rotate();
    private void FullscreenClick(object? sender, RoutedEventArgs e) => ToggleFullscreen();
    private void ViewportChanged(object? sender, SizeChangedEventArgs e) => UpdateScale();
    private void OnWheel(object? sender,PointerWheelEventArgs e) { ChangeZoom(e.Delta.Y>0?1.25:1/1.25);e.Handled=true; }
    private async void OnDrop(object? sender,DragEventArgs e) { if(e.DataTransfer.TryGetFiles()?.FirstOrDefault()?.TryGetLocalPath() is string path) await OpenAsync(path); }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.C when e.KeyModifiers == KeyModifiers.Control: CopyText(); break;
            case Key.O when e.KeyModifiers == KeyModifiers.Control: OpenClick(sender, e); break;
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
    private void PanStart(object? sender,PointerPressedEventArgs e)
    {
        if(document==null||!e.GetCurrentPoint(Viewport).Properties.IsLeftButtonPressed||(e.Source is not Image && e.Source is not TextSelectionOverlay)) return;
        int word=TextOverlay.HitWord(e.GetPosition(TextOverlay));
        if(word>=0&&(e.KeyModifiers&KeyModifiers.Shift)==0){selectingText=true;TextOverlay.SelectFrom(word);e.Pointer.Capture(Viewport);Viewport.Cursor=new Cursor(StandardCursorType.Ibeam);e.Handled=true;return;}
        TextOverlay.ClearSelection();panOrigin=e.GetPosition(Viewport);panX=Viewport.Offset.X;panY=Viewport.Offset.Y;e.Pointer.Capture(Viewport);Viewport.Cursor=new Cursor(StandardCursorType.Hand);e.Handled=true;
    }
    private void PanMove(object? sender,PointerEventArgs e)
    {
        if(selectingText){TextOverlay.ExtendTo(e.GetPosition(TextOverlay));return;}if(panOrigin is not Point start)return;var point=e.GetPosition(Viewport);Viewport.Offset=new Vector(panX+start.X-point.X,panY+start.Y-point.Y);
    }
    private void PanEnd(object? sender,PointerReleasedEventArgs e){selectingText=false;panOrigin=null;e.Pointer.Capture(null);Viewport.Cursor=null;}
    private void PanLost(object? sender,PointerCaptureLostEventArgs e){selectingText=false;panOrigin=null;Viewport.Cursor=null;}

    private void ThemeClick(object? sender,RoutedEventArgs e) => ThemeButton.ContextMenu!.Open(ThemeButton);

    private void ThemeSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.Tag is string value && Enum.TryParse<AppTheme>(value, out var choice))
        {
            themes.Select(choice); UpdateThemeMenu();
        }
    }
    private void UpdateThemeMenu()
    {
        LightThemeItem.IsChecked = themes.Preference == AppTheme.Light;
        DarkThemeItem.IsChecked = themes.Preference == AppTheme.Dark;
        SystemThemeItem.IsChecked = themes.Preference == AppTheme.System;
    }
    private async void AboutClick(object? sender,RoutedEventArgs e) => await new AboutWindow().ShowDialog(this);

    private void CopyTextClick(object? sender, RoutedEventArgs e) => CopyText();
    private void CopyAllTextClick(object? sender, RoutedEventArgs e) => CopyText(true);
    private async void CopyText(bool all = false)
    {
        string value = TextOverlay.SelectedText(all);
        if (value.Length == 0) return;
        try { if(Clipboard==null)throw new InvalidOperationException();await Clipboard.SetTextAsync(value);await Clipboard.FlushAsync(); }
        catch (Exception) { Status.Text = L.Text("CopyFailed"); }
    }
    private void QueueRecognition(ImageDocument image)
    {
        Dispatcher.UIThread.Post(new Action(() => {
            if (!closed && ReferenceEquals(document, image)) _ = RecognizeTextAsync(image);
        }),DispatcherPriority.Background);
    }
    private async Task RecognizeTextAsync(ImageDocument image)
    {
        ocrCancellation?.Cancel();
        var cancellation = new CancellationTokenSource(); ocrCancellation = cancellation;
        ocrStatus = "TextLoading"; UpdateScale();
        try
        {
            var recognized = await ocr.RecognizeAsync(image.Frames[0], L.Language, cancellation.Token);
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
