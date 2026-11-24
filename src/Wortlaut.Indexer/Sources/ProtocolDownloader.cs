using System.Runtime.CompilerServices;

namespace Wortlaut.Indexer.Sources;

public sealed class ProtocolDownloader(HttpClient http, TimeSpan pauseBetweenDownloads)
{
    private const int ListingPageSize = 10;

    private static readonly IReadOnlyDictionary<int, string> ListingIdsByPeriod = new Dictionary<int, string>
    {
        [19] = "543410-543410",
        [20] = "866354-866354",
        [21] = "1058442-1058442",
    };

    public static IEnumerable<int> SupportedPeriods => ListingIdsByPeriod.Keys;

    public async IAsyncEnumerable<DownloadedProtocol> DownloadAsync(
        int legislativePeriod,
        string targetDirectory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(targetDirectory);

        await foreach (var url in ListProtocolUrlsAsync(legislativePeriod, cancellationToken))
        {
            var path = Path.Combine(targetDirectory, ProtocolListingParser.FileName(url));
            if (File.Exists(path))
            {
                yield return new DownloadedProtocol(path, WasAlreadyPresent: true);
                continue;
            }

            await DownloadFileAsync(url, path, cancellationToken);
            yield return new DownloadedProtocol(path, WasAlreadyPresent: false);
            await Task.Delay(pauseBetweenDownloads, cancellationToken);
        }
    }

    private async IAsyncEnumerable<Uri> ListProtocolUrlsAsync(
        int legislativePeriod, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!ListingIdsByPeriod.TryGetValue(legislativePeriod, out var listingId))
        {
            throw new ArgumentOutOfRangeException(
                nameof(legislativePeriod), $"Only the legislative periods {string.Join(", ", SupportedPeriods)} are supported.");
        }

        for (var offset = 0; ; offset += ListingPageSize)
        {
            var listingUrl = $"https://www.bundestag.de/ajax/filterlist/de/services/opendata/{listingId}?offset={offset}";
            var page = await http.GetStringAsync(listingUrl, cancellationToken);
            var urls = ProtocolListingParser.ProtocolUrls(page);
            if (urls.Count == 0)
            {
                yield break;
            }

            foreach (var url in urls)
            {
                yield return url;
            }
        }
    }

    private async Task DownloadFileAsync(Uri url, string path, CancellationToken cancellationToken)
    {
        var temporaryPath = path + ".part";
        await using (var source = await http.GetStreamAsync(url, cancellationToken))
        await using (var target = File.Create(temporaryPath))
        {
            await source.CopyToAsync(target, cancellationToken);
        }

        File.Move(temporaryPath, path);
    }
}
