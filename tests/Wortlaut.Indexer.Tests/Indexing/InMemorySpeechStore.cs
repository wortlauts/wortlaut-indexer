using Wortlaut.Indexer.Indexing;
using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Tests.Indexing;

internal sealed class InMemorySpeechStore : ISpeechStore
{
    public Dictionary<string, Speech> Speeches { get; } = [];

    public int PrepareCalls { get; private set; }

    public Task PrepareAsync(CancellationToken cancellationToken)
    {
        PrepareCalls++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, string>> GetContentHashesAsync(
        int legislativePeriod, int session, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> hashes = Speeches.Values
            .Where(speech => speech.LegislativePeriod == legislativePeriod && speech.Session == session)
            .ToDictionary(speech => speech.Id, speech => speech.ContentHash!);
        return Task.FromResult(hashes);
    }

    public Task IndexAsync(IReadOnlyCollection<Speech> speeches, CancellationToken cancellationToken)
    {
        foreach (var speech in speeches)
        {
            Speeches[speech.Id] = speech;
        }

        return Task.CompletedTask;
    }
}
