namespace Wortlaut.Indexer.Model;

public sealed record Reaction(ReactionType Type, string? Source, string? Text);
