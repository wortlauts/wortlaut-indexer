using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Tests.Parsing;

public class FactionNormalizerTests
{
    [Theory]
    [InlineData("CDU/CSU", "CDU/CSU")]
    [InlineData("BÜNDNIS 90/DIE GRÜNEN", "BÜNDNIS 90/DIE GRÜNEN")]
    [InlineData("DIE LINKE", "Die Linke")]
    [InlineData("Die Linke", "Die Linke")]
    [InlineData("Fraktionslos", "fraktionslos")]
    [InlineData("fraktionslos", "fraktionslos")]
    [InlineData("Unbekannte Gruppe", "Unbekannte Gruppe")]
    public void Faction_IsMappedToItsCanonicalSpelling(string raw, string expected)
    {
        Assert.Equal(expected, FactionNormalizer.Normalize(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void MissingFaction_StaysMissing(string? raw)
    {
        Assert.Null(FactionNormalizer.Normalize(raw));
    }
}
