using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using Luma.Services;

namespace Luma;

public partial class AboutWindow : Window
{
    public AboutWindow(bool dark)
    {
        InitializeComponent();
        VersionValue.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) + " · Windows x64";
        SourceInitialized += (_, _) => WindowAppearance.ApplyDarkTitle(new WindowInteropHelper(this).Handle, dark);
    }
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
