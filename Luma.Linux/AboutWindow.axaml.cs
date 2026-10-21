using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

    public AboutWindow()
    {
        InitializeComponent();
        VersionValue.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) + " · Linux x64";
        using (var icon = Assembly.GetExecutingAssembly().GetManifestResourceStream("Luma.Icon.png")) Icon = new WindowIcon(icon!);
        L.Changed += RefreshStatus;
        Closed += (_, _) => { closed = true; L.Changed -= RefreshStatus; lifetime.Cancel(); lifetime.Dispose(); };
    }
    private void RefreshStatus()
    {
        if (statusKey != null) UpdateStatus.Text = L.Format(statusKey, releaseVersion ?? "");
    }
    private async void CheckUpdatesClick(object? sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled = false;
        releaseUrl = null;
        OpenReleaseButton.IsVisible = false;
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
            OpenReleaseButton.IsVisible = releaseUrl != null;
            RefreshStatus();
        }
        catch (OperationCanceledException) { }
        finally { if (!closed) CheckUpdatesButton.IsEnabled = true; }
    }
    private void OpenReleaseClick(object? sender, RoutedEventArgs e)
    {
        if (releaseUrl == null) return;
        try { Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true }); }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        { statusKey = "UpdateOpenFailed"; RefreshStatus(); }
    }
    private void CloseClick(object? sender, RoutedEventArgs e) => Close();
}
