using System.CommandLine;
using System.Net.Http.Headers;
using Wortlaut.Indexer.Sources;

namespace Wortlaut.Indexer.Cli;

internal static class DownloadCommand
{
    public const string DefaultTargetDirectory = "data/protocols";

    private const string ClientName = "wortlaut-indexer";
    private static readonly TimeSpan PauseBetweenDownloads = TimeSpan.FromMilliseconds(250);

    public static Command Create()
    {
        var legislativePeriod = new Option<int>("--legislative-period")
        {
            Description = $"Legislative period to download ({string.Join(", ", ProtocolDownloader.SupportedPeriods)})",
            Required = true,
        };
        var target = new Option<DirectoryInfo>("--target")
        {
            Description = "Directory to store the protocol XML files in",
            DefaultValueFactory = _ => new DirectoryInfo(DefaultTargetDirectory),
        };

        var command = new Command("download", "Download the plenary protocols of a legislative period from the Bundestag")
        {
            legislativePeriod, target,
        };

        command.SetAction((parseResult, cancellationToken) => CommandGuard.RunAsync(async () =>
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ClientName, "1.0"));
            var downloader = new ProtocolDownloader(http, PauseBetweenDownloads);

            var downloaded = 0;
            await foreach (var protocol in downloader.DownloadAsync(
                parseResult.GetRequiredValue(legislativePeriod), parseResult.GetRequiredValue(target).FullName, cancellationToken))
            {
                downloaded += protocol.WasAlreadyPresent ? 0 : 1;
                Console.WriteLine($"{(protocol.WasAlreadyPresent ? "present " : "download")} {Path.GetFileName(protocol.Path)}");
            }

            Console.WriteLine($"Done: {downloaded} protocols downloaded");
        }));

        return command;
    }
}
