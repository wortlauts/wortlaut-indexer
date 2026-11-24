using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Parsing;

public sealed record ParsedProtocol(ProtocolHeader Header, IReadOnlyList<Speech> Speeches);
