using Wortlaut.Indexer.Indexing;
using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Cli;

internal static class CommandGuard
{
    private const int Success = 0;
    private const int Failure = 1;

    public static async Task<int> RunAsync(Func<Task> command)
    {
        try
        {
            await command();
            return Success;
        }
        catch (Exception exception) when (exception is ProtocolFormatException or ElasticsearchException or HttpRequestException or IOException)
        {
            await Console.Error.WriteLineAsync(exception.Message);
            return Failure;
        }
    }
}
