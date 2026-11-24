using System.Security.Cryptography;
using System.Text.Json;
using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Indexing;

public static class ContentHash
{
    public static Speech Seal(Speech speech) => speech with { ContentHash = Compute(speech) };

    private static string Compute(Speech speech)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(speech with { ContentHash = null }, SpeechJson.Options);
        return Convert.ToHexStringLower(SHA256.HashData(json));
    }
}
