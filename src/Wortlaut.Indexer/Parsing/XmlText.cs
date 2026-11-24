using System.Xml.Linq;

namespace Wortlaut.Indexer.Parsing;

internal static class XmlText
{
    public static string PlainText(XElement element) =>
        TextNormalizer.Normalize(string.Concat(TextNodes(element)));

    public static string? ValueOrNull(XElement? element)
    {
        var text = element is null ? string.Empty : PlainText(element);
        return text.Length == 0 ? null : text;
    }

    private static IEnumerable<string> TextNodes(XNode node) => node switch
    {
        XText text => [text.Value],
        XElement { Name.LocalName: "fussnote" } => [],
        XElement element => element.Nodes().SelectMany(TextNodes),
        _ => [],
    };
}
