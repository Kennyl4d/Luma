using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Luma.Services;

namespace Luma.Imaging;

internal static class SvgInput
{
    public static byte[] Read(string path)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings {
            // Illustrator exports commonly include the SVG 1.1 PUBLIC doctype.
            // Ignore it instead of rejecting the image; never load DTDs or expand their entities.
            DtdProcessing = DtdProcessing.Ignore, XmlResolver = null,
            MaxCharactersInDocument = 16 * 1024 * 1024 });
        var document = XDocument.Load(reader);
        // Processing instructions such as xml-stylesheet are not part of the drawing.
        document.DescendantNodes().OfType<XProcessingInstruction>().Remove();
        var root = document.Root;
        if (root == null || root.Name != XName.Get("svg", "http://www.w3.org/2000/svg")) Reject();
        foreach (var element in root!.DescendantsAndSelf())
        {
            if (element.Name.LocalName is "script" or "foreignObject") Reject();
            foreach (var attribute in element.Attributes())
            {
                string name = attribute.Name.LocalName;
                if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) || attribute.Name == XNamespace.Xml + "base") Reject();
                if (name is "href" or "src")
                {
                    string value = attribute.Value.Trim();
                    if (!value.StartsWith('#') && !Regex.IsMatch(value, @"\Adata:image/(png|jpeg|webp);base64,[A-Za-z0-9+/=\s]+\z", RegexOptions.IgnoreCase)) Reject();
                }
                CheckCss(attribute.Value);
            }
            if (element.Name.LocalName == "style") CheckCss(element.Value);
        }
        return Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting));
    }

    private static void CheckCss(string value)
    {
        // Escaped CSS and imports may conceal external URLs. Local fragment paint references are allowed.
        if (value.Contains('@') || value.Contains('\\')) Reject();
        foreach (Match match in Regex.Matches(value, @"url\s*\(([^)]*)\)", RegexOptions.IgnoreCase))
            if (!match.Groups[1].Value.Trim().Trim('\'', '"').StartsWith('#')) Reject();
    }
    private static void Reject() => throw new InvalidDataException(L.Text("UnsupportedFormat"));
}
