using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;

namespace Luma.Services;

public static class L
{
    private sealed record Catalog(string[] Keys, Dictionary<string, string[]> Languages);
    private static readonly Catalog catalog = Load();
    private static Dictionary<string, string> current = new();
    public static string Language { get; private set; } = "en";
    public static event Action? Changed;
    public static string Text(string key) => current.TryGetValue(key, out var value) ? value : key;
    public static string Format(string key, params object[] arguments) => string.Format(CultureInfo.CurrentCulture, Text(key), arguments);

    public static void Refresh()
    {
        string name;
        try { name = new[]{Environment.GetEnvironmentVariable("LANGUAGE")?.Split(':')[0],Environment.GetEnvironmentVariable("LC_ALL"),Environment.GetEnvironmentVariable("LC_MESSAGES"),Environment.GetEnvironmentVariable("LANG"),CultureInfo.CurrentUICulture.Name}.FirstOrDefault(value=>!string.IsNullOrWhiteSpace(value))?.Split('.')[0].Split('@')[0].Replace('_','-')??"en"; if (name is "C" or "POSIX") name = "en"; }
        catch (CultureNotFoundException) { name = CultureInfo.CurrentUICulture.Name; }
        string language = Resolve(name);
        if (language == Language && current.Count != 0) return;
        Language = language;
        var values = catalog.Languages[language];
        current = catalog.Keys.Zip(values).ToDictionary(pair => pair.First, pair => pair.Second);
        current["Description"]=current["Description"].Replace("Windows","Linux",StringComparison.Ordinal);
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(language);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
        foreach (var pair in current) Application.Current!.Resources["L." + pair.Key] = pair.Value;
        Changed?.Invoke();
    }

    public static string Resolve(string name)
    {
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return name.Contains("Hant", StringComparison.OrdinalIgnoreCase) || name.EndsWith("TW", StringComparison.OrdinalIgnoreCase) || name.EndsWith("HK", StringComparison.OrdinalIgnoreCase) || name.EndsWith("MO", StringComparison.OrdinalIgnoreCase) ? "zh-Hant" : "zh-Hans";
        if (name.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return "pt-BR";
        string primary = name.Split('-')[0].ToLowerInvariant();
        return catalog.Languages.ContainsKey(primary) ? primary : "en";
    }

    private static Catalog Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Luma.Resources.Strings.json")!;
        var result = JsonSerializer.Deserialize<Catalog>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        if (result.Languages.Any(pair => pair.Value.Length != result.Keys.Length)) throw new InvalidOperationException("Incomplete localization catalog");
        return result;
    }
}

