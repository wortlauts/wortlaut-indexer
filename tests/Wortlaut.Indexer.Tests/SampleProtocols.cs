namespace Wortlaut.Indexer.Tests;

internal static class SampleProtocols
{
    public const string Period21 = "21-108.sample.xml";
    public const string Period20 = "20-214.sample.xml";
    public const string Period19 = "19-239.sample.xml";

    public static string DirectoryPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "testdata", "protocols");

    public static string PathOf(string fileName) => Path.Combine(DirectoryPath, fileName);
}
