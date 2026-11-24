using System.Net;
using Wortlaut.Indexer.Sources;

namespace Wortlaut.Indexer.Tests.Sources;

public class ProtocolDownloaderTests
{
    private const string FirstProtocolUrl = "https://www.bundestag.de/resource/blob/1/21001.xml";
    private const string SecondProtocolUrl = "https://www.bundestag.de/resource/blob/2/21002.xml";

    [Fact]
    public async Task AllListingPages_AreDownloadedUntilAnEmptyPageAppears()
    {
        using var directory = new TemporaryDirectory();
        var handler = new StubHandler();
        var downloader = new ProtocolDownloader(new HttpClient(handler), TimeSpan.Zero);

        var downloads = await downloader.DownloadAsync(21, directory.Path, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["21001.xml", "21002.xml"], downloads.Select(download => Path.GetFileName(download.Path)));
        Assert.All(downloads, download => Assert.False(download.WasAlreadyPresent));
        Assert.Equal("<protocol 21001/>", await File.ReadAllTextAsync(Path.Combine(directory.Path, "21001.xml"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProtocolsThatAreAlreadyPresent_AreNotDownloadedAgain()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "21001.xml"), "existing", TestContext.Current.CancellationToken);
        var handler = new StubHandler();
        var downloader = new ProtocolDownloader(new HttpClient(handler), TimeSpan.Zero);

        var downloads = await downloader.DownloadAsync(21, directory.Path, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.True(downloads[0].WasAlreadyPresent);
        Assert.DoesNotContain(FirstProtocolUrl, handler.RequestedUrls);
        Assert.Equal("existing", await File.ReadAllTextAsync(downloads[0].Path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnsupportedPeriod_IsRejected()
    {
        using var directory = new TemporaryDirectory();
        var downloader = new ProtocolDownloader(new HttpClient(new StubHandler()), TimeSpan.Zero);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await downloader.DownloadAsync(5, directory.Path, TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<string> RequestedUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            RequestedUrls.Add(url);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ContentOf(url)) });
        }

        private static string ContentOf(string url) => url switch
        {
            _ when url.EndsWith("offset=0") => $"""<a href="{FirstProtocolUrl}">XML</a><a href="{SecondProtocolUrl}">XML</a>""",
            _ when url.Contains("offset=") => "<p>no more protocols</p>",
            _ => $"<protocol {Path.GetFileNameWithoutExtension(url)}/>",
        };
    }
}
