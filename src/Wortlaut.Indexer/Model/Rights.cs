namespace Wortlaut.Indexer.Model;

public sealed record Rights(string License, string Attribution, IReadOnlyList<string> AllowedActions)
{
    public static Rights OfficialWork { get; } = new(
        License: "Official work (section 5 UrhG)",
        Attribution: "Deutscher Bundestag",
        AllowedActions: ["read", "quote"]);
}
