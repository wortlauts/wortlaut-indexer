namespace Wortlaut.Indexer.Indexing;

internal static class IndexTemplate
{
    private const string ResourceName = "index-template.json";

    public static string Json { get; } = Load();

    private static string Load()
    {
        using var stream = typeof(IndexTemplate).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
