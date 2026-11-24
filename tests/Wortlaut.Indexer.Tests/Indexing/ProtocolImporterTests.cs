using Wortlaut.Indexer.Indexing;
using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Tests.Indexing;

public class ProtocolImporterTests
{
    private readonly InMemorySpeechStore store = new();
    private readonly CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task AllSpeechesOfAProtocol_AreWrittenToTheStore()
    {
        var results = await Import(SampleProtocols.Period21);

        var result = Assert.Single(results);
        Assert.Equal(3, result.SpeechCount);
        Assert.Equal(3, result.WrittenCount);
        Assert.Equal(3, store.Speeches.Count);
        Assert.All(store.Speeches.Values, speech => Assert.NotNull(speech.ContentHash));
    }

    [Fact]
    public async Task ImportingTheSameProtocolAgain_WritesNothing()
    {
        await Import(SampleProtocols.Period21);

        var results = await Import(SampleProtocols.Period21);

        var result = Assert.Single(results);
        Assert.Equal(3, result.SpeechCount);
        Assert.Equal(0, result.WrittenCount);
    }

    [Fact]
    public async Task OnlyChangedSpeeches_AreWrittenAgain()
    {
        await Import(SampleProtocols.Period21);
        var changedId = store.Speeches.Keys.Order().First();
        store.Speeches[changedId] = store.Speeches[changedId] with { ContentHash = "outdated" };

        var results = await Import(SampleProtocols.Period21);

        Assert.Equal(1, Assert.Single(results).WrittenCount);
        Assert.NotEqual("outdated", store.Speeches[changedId].ContentHash);
    }

    [Fact]
    public async Task ProtocolsOfOtherLegislativePeriods_AreSkipped()
    {
        var results = await Import(period: 20, SampleProtocols.Period21, SampleProtocols.Period20);

        var result = Assert.Single(results);
        Assert.Equal(20, result.Header.LegislativePeriod);
        Assert.All(store.Speeches.Values, speech => Assert.Equal(20, speech.LegislativePeriod));
    }

    [Fact]
    public async Task WithoutPeriodFilter_AllProtocolsAreImported()
    {
        var results = await Import(period: null, SampleProtocols.Period21, SampleProtocols.Period20, SampleProtocols.Period19);

        Assert.Equal([21, 20, 19], results.Select(result => result.Header.LegislativePeriod));
        Assert.Equal(9, store.Speeches.Count);
    }

    [Fact]
    public async Task TheStoreIsPreparedOnceBeforeTheFirstProtocol()
    {
        await Import(period: null, SampleProtocols.Period21, SampleProtocols.Period20);

        Assert.Equal(1, store.PrepareCalls);
    }

    [Fact]
    public async Task MalformedProtocol_IsReportedWithItsFileName()
    {
        using var directory = new TemporaryDirectory();
        var brokenFile = Path.Combine(directory.Path, "broken.xml");
        await File.WriteAllTextAsync(brokenFile, "<dbtplenarprotokoll wahlperiode=\"21\"/>", cancellationToken);

        var exception = await Assert.ThrowsAsync<ProtocolFormatException>(async () =>
            await new ProtocolImporter(store).ImportAsync([brokenFile], null, cancellationToken).ToListAsync(cancellationToken));

        Assert.StartsWith("broken.xml:", exception.Message);
    }

    private Task<List<ProtocolImportResult>> Import(string sampleFile) => Import(period: null, sampleFile);

    private async Task<List<ProtocolImportResult>> Import(int? period, params string[] sampleFiles) =>
        await new ProtocolImporter(store)
            .ImportAsync(sampleFiles.Select(SampleProtocols.PathOf), period, cancellationToken)
            .ToListAsync(cancellationToken);
}
