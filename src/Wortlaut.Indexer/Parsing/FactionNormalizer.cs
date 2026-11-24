namespace Wortlaut.Indexer.Parsing;

internal static class FactionNormalizer
{
    private static readonly string[] CanonicalFactions =
    [
        "CDU/CSU",
        "SPD",
        "AfD",
        "FDP",
        "BSW",
        "BÜNDNIS 90/DIE GRÜNEN",
        "Die Linke",
        "fraktionslos",
    ];

    public static string? Normalize(string? faction)
    {
        if (string.IsNullOrWhiteSpace(faction))
        {
            return null;
        }

        var normalized = TextNormalizer.Normalize(faction);
        return CanonicalFactions.FirstOrDefault(canonical =>
            string.Equals(canonical, normalized, StringComparison.OrdinalIgnoreCase)) ?? normalized;
    }
}
