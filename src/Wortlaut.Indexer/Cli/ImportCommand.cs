using System.CommandLine;
using Wortlaut.Indexer.Indexing;

namespace Wortlaut.Indexer.Cli;

internal static class ImportCommand
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(2);

    public static Command Create()
    {
        var source = new Option<DirectoryInfo>("--source")
        {
            Description = "Directory with the protocol XML files",
            DefaultValueFactory = _ => new DirectoryInfo(DownloadCommand.DefaultTargetDirectory),
        };
        var legislativePeriod = new Option<int?>("--legislative-period")
        {
            Description = "Only import protocols of this legislative period",
        };
        var elasticsearch = new Option<Uri>("--elasticsearch")
        {
            Description = "Elasticsearch base URL",
            DefaultValueFactory = _ => new Uri("http://localhost:9200"),
        };
        var index = new Option<string>("--index")
        {
            Description = "Index to write to",
            DefaultValueFactory = _ => new IndexNames().Index,
        };
        var alias = new Option<string>("--alias")
        {
            Description = "Alias that points to the index",
            DefaultValueFactory = _ => new IndexNames().Alias,
        };

        var command = new Command("import", "Parse protocols and write their speeches to Elasticsearch")
        {
            source, legislativePeriod, elasticsearch, index, alias,
        };

        command.SetAction((parseResult, cancellationToken) => CommandGuard.RunAsync(async () =>
        {
            using var http = new HttpClient { BaseAddress = parseResult.GetRequiredValue(elasticsearch), Timeout = RequestTimeout };
            var names = new IndexNames(parseResult.GetRequiredValue(index), parseResult.GetRequiredValue(alias));
            var importer = new ProtocolImporter(new ElasticsearchSpeechStore(http, names));
            var files = parseResult.GetRequiredValue(source).EnumerateFiles("*.xml").Select(file => file.FullName).Order();

            var protocols = 0;
            var written = 0;
            await foreach (var result in importer.ImportAsync(files, parseResult.GetValue(legislativePeriod), cancellationToken))
            {
                protocols++;
                written += result.WrittenCount;
                Console.WriteLine(
                    $"{result.Header.LegislativePeriod}/{result.Header.Session}: {result.WrittenCount} of {result.SpeechCount} speeches written");
            }

            Console.WriteLine($"Done: {protocols} protocols, {written} speeches written");
        }));

        return command;
    }
}
