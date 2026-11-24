namespace Wortlaut.Indexer.Indexing;

public sealed class ElasticsearchException(string message) : Exception(message);
