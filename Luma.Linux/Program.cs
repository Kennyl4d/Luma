using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Luma.Services;

namespace Luma;
internal static class Program
{
 [STAThread] public static void Main(string[] args) => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().StartWithClassicDesktopLifetime(args);
}
public sealed class App : Application
{
 public override void Initialize() { Styles.Add(new FluentTheme()); L.Refresh(); }
 public override void OnFrameworkInitializationCompleted()
 {
  if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new MainWindow(desktop.Args?.FirstOrDefault());
  base.OnFrameworkInitializationCompleted();
 }
}
