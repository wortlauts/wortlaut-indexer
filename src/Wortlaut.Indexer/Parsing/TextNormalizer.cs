using System.Text.RegularExpressions;

namespace Wortlaut.Indexer.Parsing;

internal static partial class TextNormalizer
{
    public static string Normalize(string text) => AnyWhitespace().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex AnyWhitespace();
}
