using System.Runtime.CompilerServices;
using System.Xml;
using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Indexing;

public sealed class ProtocolImporter(ISpeechStore store)
{
    public async IAsyncEnumerable<ProtocolImportResult> ImportAsync(
        IEnumerable<string> protocolFiles,
        int? legislativePeriod,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await store.PrepareAsync(cancellationToken);

        foreach (var file in protocolFiles)
        {
            var protocol = Parse(file);
            if (legislativePeriod is null || protocol.Header.LegislativePeriod == legislativePeriod)
            {
                yield return await ImportProtocolAsync(protocol, cancellationToken);
            }
        }
    }

    private async Task<ProtocolImportResult> ImportProtocolAsync(ParsedProtocol protocol, CancellationToken cancellationToken)
    {
        var speeches = protocol.Speeches.Select(ContentHash.Seal).ToList();
        var knownHashes = await store.GetContentHashesAsync(
            protocol.Header.LegislativePeriod, protocol.Header.Session, cancellationToken);

        var changed = speeches
            .Where(speech => !knownHashes.TryGetValue(speech.Id, out var hash) || hash != speech.ContentHash)
            .ToList();

        if (changed.Count > 0)
        {
            await store.IndexAsync(changed, cancellationToken);
        }

        return new ProtocolImportResult(protocol.Header, speeches.Count, changed.Count);
    }

    private static ParsedProtocol Parse(string file)
    {
        try
        {
            return ProtocolParser.ParseFile(file);
        }
        catch (Exception exception) when (exception is ProtocolFormatException or XmlException)
        {
            throw new ProtocolFormatException($"{Path.GetFileName(file)}: {exception.Message}");
        }
    }
}
