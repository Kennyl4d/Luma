using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using Luma.Services;

namespace Luma;

public partial class AboutWindow : Window
{
    private readonly CancellationTokenSource lifetime = new();
    private readonly UpdateService updates = new();
    private string? statusKey;
    private string? releaseVersion;
    private string? releaseUrl;
    private bool closed;

    public AboutWindow(bool dark)
    {
        InitializeComponent();
        VersionValue.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) + " · Windows x64";
        SourceInitialized += (_, _) => WindowAppearance.ApplyDarkTitle(new WindowInteropHelper(this).Handle, dark);
        L.Changed += RefreshStatus;
        Closed += (_, _) => { closed = true; L.Changed -= RefreshStatus; lifetime.Cancel(); lifetime.Dispose(); };
    }
    private void RefreshStatus()
    {
        if (statusKey != null) UpdateStatus.Text = L.Format(statusKey, releaseVersion ?? "");
    }
    private async void CheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled = false;
        releaseUrl = null;
        OpenReleaseButton.Visibility = Visibility.Collapsed;
        statusKey = "UpdateChecking"; RefreshStatus();
        try
        {
            var result = await updates.CheckAsync(Assembly.GetExecutingAssembly().GetName().Version!, lifetime.Token);
            if (closed) return;
            releaseVersion = result.Version; releaseUrl = result.ReleaseUrl;
            statusKey = result.State switch {
                UpdateState.Available => "UpdateAvailable",
                UpdateState.Current => "UpdateCurrent",
                UpdateState.Unavailable => "UpdateUnavailable",
                _ => "UpdateFailed" };
            OpenReleaseButton.Visibility = releaseUrl == null ? Visibility.Collapsed : Visibility.Visible;
            RefreshStatus();
        }
        catch (OperationCanceledException) { }
        finally { if (!closed) CheckUpdatesButton.IsEnabled = true; }
    }
    private void OpenReleaseClick(object sender, RoutedEventArgs e)
    {
        if (releaseUrl == null) return;
        try { Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true }); }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        { statusKey = "UpdateOpenFailed"; RefreshStatus(); }
    }
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
