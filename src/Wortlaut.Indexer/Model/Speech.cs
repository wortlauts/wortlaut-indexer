namespace Wortlaut.Indexer.Model;

public sealed record Speech(
    string Id,
    int LegislativePeriod,
    int Session,
    DateOnly Date,
    string? AgendaItem,
    SpeechDelivery Delivery,
    Speaker Speaker,
    string Text,
    IReadOnlyList<Reaction> Reactions,
    int WordCount,
    string SourceUrl,
    Rights Rights,
    string? ContentHash = null);
