using System;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class Language
{
    public string key { get; set; }
    public string culture { get; set; }
    public int id { get; set; }
    public string[] strings { get; set; }
}

internal static class Setup
{
    [DllImport("kernel32.dll")] private static extern ushort GetUserDefaultUILanguage();

    [STAThread]
    private static int Main()
    {
        string failure = "Unable to start Luma setup. Try running the installer again.";
        string directory = null;
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            Language[] languages;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream("Languages.json")))
                languages = new JavaScriptSerializer().Deserialize<Language[]>(reader.ReadToEnd());
            string name = CultureInfo.GetCultureInfo(GetUserDefaultUILanguage()).Name;
            string primary = name.Split('-')[0].ToLowerInvariant();
            string key = primary == "pt" ? "pt-BR" : primary;
            if (primary == "zh")
                key = name.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.EndsWith("TW", StringComparison.OrdinalIgnoreCase) || name.EndsWith("HK", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("MO", StringComparison.OrdinalIgnoreCase) ? "zh-Hant" : "zh-Hans";
            var language = languages.FirstOrDefault(item => item.key == key) ?? languages.First(item => item.key == "en");
            failure = language.strings[5];
            string hash;
            using (var resource = assembly.GetManifestResourceStream("Luma.msi"))
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(resource)).Replace("-", "");
            // Keep the original MSI source for future repair. Its embedded transforms are cached by Windows Installer.
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Luma", "Installer", hash);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "Luma.msi");
            bool valid = false;
            if (File.Exists(path))
                using (var file = File.OpenRead(path))
                using (var sha = SHA256.Create()) valid = hash == BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
            if (!valid)
            {
                string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
                using (var resource = assembly.GetManifestResourceStream("Luma.msi"))
                using (var file = File.Create(temporary)) resource.CopyTo(file);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
            }
            string log = Path.Combine(directory, "setup.log");
            string properties = language.key == "en" ? "" : " TRANSFORMS=\":Luma_" + language.culture + "\"";
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msiexec.exe"),
                "/i \"" + path + "\"" + properties + " /L*V \"" + log + "\"") { UseShellExecute = false };
            using (var process = Process.Start(start))
            {
                if (process == null) throw new InvalidOperationException("Windows Installer did not start.");
                process.WaitForExit();
                int result = process.ExitCode;
                if (result != 0 && result != 3010 && result != 1641 && result != 1602)
                    MessageBox.Show(failure + "\n\n" + result + "\n" + log, "Luma", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return result;
            }
        }
        catch (Exception error)
        {
            if (directory != null)
                try { File.WriteAllText(Path.Combine(directory, "bootstrap-error.log"), error.ToString()); } catch { }
            MessageBox.Show(failure, "Luma", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1603;
        }
    }
}
