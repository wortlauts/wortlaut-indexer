using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Indexing;

public interface ISpeechStore
{
    Task PrepareAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> GetContentHashesAsync(
        int legislativePeriod, int session, CancellationToken cancellationToken);

    Task IndexAsync(IReadOnlyCollection<Speech> speeches, CancellationToken cancellationToken);
}
