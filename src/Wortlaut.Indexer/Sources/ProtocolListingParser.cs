using System.Text.RegularExpressions;

namespace Wortlaut.Indexer.Sources;

internal static partial class ProtocolListingParser
{
    public static IReadOnlyList<Uri> ProtocolUrls(string listingHtml) =>
        ProtocolLink().Matches(listingHtml)
            .Select(match => new Uri(match.Value))
            .DistinctBy(FileName)
            .ToList();

    public static string FileName(Uri protocolUrl) => protocolUrl.Segments[^1];

    [GeneratedRegex(@"https://www\.bundestag\.de/resource/blob/\d+/\d+\.xml")]
    private static partial Regex ProtocolLink();
}
