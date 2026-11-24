using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Tests;

internal static class TestSpeeches
{
    public static Speech Create(
        string id = "21-108-00100",
        int legislativePeriod = 21,
        int session = 108,
        string text = "Ein Satz über die Verwaltung.",
        string? contentHash = null) => new(
            Id: id,
            LegislativePeriod: legislativePeriod,
            Session: session,
            Date: new DateOnly(2026, 11, 12),
            AgendaItem: "Haushaltsgesetz",
            Delivery: SpeechDelivery.Plenary,
            Speaker: new Speaker("42", "Dr. Anna Muster", "Die Linke", "Bundesministerin BMF"),
            Text: text,
            Reactions:
            [
                new Reaction(ReactionType.Applause, "Die Linke", Text: null),
                new Reaction(ReactionType.Heckle, "Max Frager [AfD]", "Das stimmt nicht!"),
            ],
            WordCount: text.Split(' ').Length,
            SourceUrl: "https://dserver.bundestag.de/btp/21/21108.pdf",
            Rights: Rights.OfficialWork,
            ContentHash: contentHash);
}
