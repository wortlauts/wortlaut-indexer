using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Indexing;

public sealed record ProtocolImportResult(ProtocolHeader Header, int SpeechCount, int WrittenCount);
