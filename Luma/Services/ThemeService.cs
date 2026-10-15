using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Luma.Services;

public enum AppTheme { System, Light, Dark }

public sealed class ThemeService
{
    private readonly string preferenceFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Luma", "theme.txt");
    public AppTheme Preference { get; private set; } = AppTheme.System;
    public bool IsDark { get; private set; }

    public ThemeService()
    {
        try
        {
            if (File.Exists(preferenceFile) && Enum.TryParse<AppTheme>(File.ReadAllText(preferenceFile), out var stored) && Enum.IsDefined(stored)) Preference = stored;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException) { }
        Refresh();
    }

    public void Select(AppTheme theme)
    {
        Preference = theme;
        Refresh();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(preferenceFile)!);
            var temporary = preferenceFile + ".tmp";
            File.WriteAllText(temporary, theme.ToString());
            File.Move(temporary, preferenceFile, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException) { }
    }

    public void Refresh()
    {
        IsDark = Preference == AppTheme.Dark || (Preference == AppTheme.System && SystemIsDark());
        var palette = IsDark
            ? new[] { "#191919", "#252525", "#ECECEC", "#BDBDBD", "#353535", "#454545", "#191919", "#666666", "#929292" }
            : new[] { "#FAFAFA", "#F0F0F0", "#202020", "#5C5C5C", "#E2E2E2", "#D0D0D0", "#F0F0F0", "#9A9A9A", "#666666" };
        string[] keys = ["CanvasBrush", "PanelBrush", "TextBrush", "MutedBrush", "HoverBrush", "FocusBrush", "ScrollTrackBrush", "ScrollThumbBrush", "ScrollThumbHoverBrush"];
        for (int i = 0; i < keys.Length; i++)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(palette[i]));
            brush.Freeze();
            Application.Current.Resources[keys[i]] = brush;
        }
    }

    private static bool SystemIsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException) { return false; }
    }
}
