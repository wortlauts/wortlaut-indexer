using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Wortlaut.Indexer.Parsing;

internal static partial class AgendaItemReader
{
    private const string TitleClass = "T_fett";
    private const string DescriptionClass = "T_NaS";
    private const string TitleSeparator = " / ";

    public static string? Read(XElement? topic)
    {
        if (topic is null)
        {
            return null;
        }

        var titles = ParagraphTexts(topic, TitleClass).ToList();
        if (titles.Count > 0)
        {
            return string.Join(TitleSeparator, titles);
        }

        var description = ParagraphTexts(topic, DescriptionClass).FirstOrDefault();
        return description is null
            ? (string?)topic.Attribute("top-id")
            : ListMarker().Replace(description, string.Empty);
    }

    private static IEnumerable<string> ParagraphTexts(XElement topic, string paragraphClass) =>
        topic.Elements("p")
            .Where(paragraph => (string?)paragraph.Attribute("klasse") == paragraphClass)
            .Select(XmlText.PlainText);

    [GeneratedRegex(@"^(?:[–-]\s*|[a-z]\)\s*)+")]
    private static partial Regex ListMarker();
}
