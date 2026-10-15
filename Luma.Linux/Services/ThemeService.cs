using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
namespace Luma.Services;
public enum AppTheme { System,Light,Dark }
public sealed class ThemeService
{
 private readonly string file=Path.Combine(ConfigDirectory(),"luma","theme.txt");
 private static string ConfigDirectory(){string? value=Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");return !string.IsNullOrWhiteSpace(value)&&Path.IsPathRooted(value)?value:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".config");}
 public AppTheme Preference {get;private set;}=AppTheme.System;
 public ThemeService() { try { if(File.Exists(file)&&Enum.TryParse<AppTheme>(File.ReadAllText(file),out var stored)&&Enum.IsDefined(stored))Preference=stored; }catch(IOException){}catch(UnauthorizedAccessException){} Apply(); }
 public void Select(AppTheme value) { Preference=value; Apply();try{Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file+".tmp",value.ToString());File.Move(file+".tmp",file,true);}catch(IOException){}catch(UnauthorizedAccessException){} }
 private void Apply() { Application.Current!.RequestedThemeVariant=Preference switch {AppTheme.Light=>ThemeVariant.Light,AppTheme.Dark=>ThemeVariant.Dark,_=>ThemeVariant.Default};RefreshColors(); }
 public void RefreshColors()
 {
  bool dark=Preference==AppTheme.Dark||(Preference==AppTheme.System&&Application.Current!.ActualThemeVariant==ThemeVariant.Dark);
  var colors=dark?new[]{"#191919","#252525","#ECECEC","#BDBDBD","#353535","#454545","#191919","#666666","#929292"}:new[]{"#FAFAFA","#F0F0F0","#202020","#5C5C5C","#E2E2E2","#D0D0D0","#F0F0F0","#9A9A9A","#666666"};
  string[] keys=["CanvasBrush","PanelBrush","TextBrush","MutedBrush","HoverBrush","FocusBrush","ScrollTrackBrush","ScrollThumbBrush","ScrollThumbHoverBrush"];
  for(int i=0;i<keys.Length;i++)Application.Current!.Resources[keys[i]]=new SolidColorBrush(Color.Parse(colors[i]));
 }
}
