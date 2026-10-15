using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Reflection;
namespace Luma;
public partial class AboutWindow:Window
{
 public AboutWindow(){InitializeComponent();VersionValue.Text=Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)+" · Linux x64";using(var icon=Assembly.GetExecutingAssembly().GetManifestResourceStream("Luma.Icon.png"))Icon=new WindowIcon(icon!);}
 private void CloseClick(object? sender,RoutedEventArgs e)=>Close();
}
