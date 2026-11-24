using System.Text;
using System.Text.Json;
using Wortlaut.Indexer.Indexing;
using Wortlaut.Indexer.Model;
using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Tests.Indexing;

[Trait("Category", "Integration")]
public class ElasticsearchSpeechStoreTests : IClassFixture<ElasticsearchFixture>, IDisposable
{
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;
    private readonly HttpClient http;
    private readonly IndexNames names;
    private readonly ElasticsearchSpeechStore store;

    public ElasticsearchSpeechStoreTests(ElasticsearchFixture elasticsearch)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        names = new IndexNames($"speeches-test-{suffix}", $"speeches-alias-{suffix}");
        http = elasticsearch.CreateClient();
        store = new ElasticsearchSpeechStore(http, names);
    }

    public void Dispose() => http.Dispose();

    [Fact]
    public async Task Prepare_CreatesTheIndexBehindTheAlias()
    {
        await store.PrepareAsync(cancellationToken);

        var response = await Get($"_alias/{names.Alias}");
        Assert.True(response.RootElement.TryGetProperty(names.Index, out _));
    }

    [Fact]
    public async Task Prepare_CanRunRepeatedly()
    {
        await store.PrepareAsync(cancellationToken);

        await store.PrepareAsync(cancellationToken);
    }

    [Fact]
    public async Task IndexedSpeeches_AreSearchableThroughTheAlias()
    {
        await store.PrepareAsync(cancellationToken);

        await IndexAndRefresh(TestSpeeches.Create(id: "21-108-00100"), TestSpeeches.Create(id: "21-108-00200"));

        Assert.Equal(2, await CountHits(names.Alias, new { match_all = new { } }));
    }

    [Fact]
    public async Task ContentHashes_AreReturnedPerSession()
    {
        await store.PrepareAsync(cancellationToken);
        await IndexAndRefresh(
            ContentHash.Seal(TestSpeeches.Create(id: "21-108-00100", session: 108)),
            ContentHash.Seal(TestSpeeches.Create(id: "21-109-00100", session: 109)));

        var hashes = await store.GetContentHashesAsync(21, 108, cancellationToken);

        var (id, hash) = Assert.Single(hashes);
        Assert.Equal("21-108-00100", id);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public async Task ParsedSampleProtocols_FitTheStrictMapping()
    {
        await store.PrepareAsync(cancellationToken);
        var speeches = new[] { SampleProtocols.Period21, SampleProtocols.Period20, SampleProtocols.Period19 }
            .SelectMany(file => ProtocolParser.ParseFile(SampleProtocols.PathOf(file)).Speeches)
            .Select(ContentHash.Seal)
            .ToArray();

        await IndexAndRefresh(speeches);

        Assert.Equal(speeches.Length, await CountHits(names.Alias, new { match_all = new { } }));
    }

    [Fact]
    public async Task StemmedField_FindsOtherInflectionsWhileTheExactFieldDoesNot()
    {
        await store.PrepareAsync(cancellationToken);
        await IndexAndRefresh(TestSpeeches.Create(text: "Die Verwaltungen sparen Geld."));

        var stemmed = await CountHits(names.Alias, new { match = new Dictionary<string, string> { ["text"] = "Verwaltung" } });
        var exactMiss = await CountHits(names.Alias, new { match_phrase = new Dictionary<string, string> { ["text.exact"] = "Verwaltung" } });
        var exactHit = await CountHits(names.Alias, new { match_phrase = new Dictionary<string, string> { ["text.exact"] = "Verwaltungen sparen" } });

        Assert.Equal((1, 0, 1), (stemmed, exactMiss, exactHit));
    }

    [Fact]
    public async Task SpeechesWithAnUnmappedField_AreRejected()
    {
        await store.PrepareAsync(cancellationToken);
        var body = $"{{\"index\":{{\"_index\":\"{names.Index}\",\"_id\":\"x\"}}}}\n{{\"unknown_field\":1}}\n";
        using var content = new StringContent(body, Encoding.UTF8, "application/x-ndjson");

        using var response = await http.PostAsync("_bulk", content, cancellationToken);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        Assert.True(result.RootElement.GetProperty("errors").GetBoolean());
    }

    [Fact]
    public async Task RejectedSpeeches_AreReportedAsAnError()
    {
        await store.PrepareAsync(cancellationToken);
        var outOfRange = TestSpeeches.Create(legislativePeriod: 40_000);

        var exception = await Assert.ThrowsAsync<ElasticsearchException>(() => store.IndexAsync([outOfRange], cancellationToken));

        Assert.Contains("1 speeches were rejected", exception.Message);
    }

    private async Task IndexAndRefresh(params Speech[] speeches)
    {
        await store.IndexAsync(speeches, cancellationToken);
        using var response = await http.PostAsync($"{names.Index}/_refresh", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<JsonDocument> Get(string path)
    {
        using var response = await http.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task<int> CountHits(string target, object query)
    {
        using var content = new StringContent(JsonSerializer.Serialize(new { query }), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync($"{target}/_count", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return result.RootElement.GetProperty("count").GetInt32();
    }
}
