using System.Net;
using System.Text;
using System.Text.Json;
using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Indexing;

public sealed class EsSpeechStore(HttpClient http, IndexNames names) : ISpeechStore
{
    private const string TemplateName = "wortlaut-speeches";
    private const string JsonContentType = "application/json";
    private const string NdjsonContentType = "application/x-ndjson";
    private const int BulkBatchSize = 100;
    private const int MaxSpeechesPerSession = 10_000;
    private const int MaxReportedBulkFailures = 3;

    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        await SendAsync(HttpMethod.Put, $"_index_template/{TemplateName}", IndexTemplate.Json, JsonContentType, cancellationToken);

        if (!await IndexExistsAsync(cancellationToken))
        {
            await SendAsync(HttpMethod.Put, names.Index, body: null, JsonContentType, cancellationToken);
        }

        await SendAsync(HttpMethod.Put, $"{names.Index}/_alias/{names.Alias}", body: null, JsonContentType, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetContentHashesAsync(
        int legislativePeriod, int session, CancellationToken cancellationToken)
    {
        var query = new
        {
            size = MaxSpeechesPerSession,
            _source = new[] { "content_hash" },
            query = new
            {
                @bool = new
                {
                    filter = new object[]
                    {
                        new { term = new Dictionary<string, int> { ["legislative_period"] = legislativePeriod } },
                        new { term = new Dictionary<string, int> { ["session"] = session } },
                    },
                },
            },
        };

        var response = await SendAsync(
            HttpMethod.Post, $"{names.Index}/_search", JsonSerializer.Serialize(query), JsonContentType, cancellationToken);

        using var document = JsonDocument.Parse(response);
        return document.RootElement.GetProperty("hits").GetProperty("hits").EnumerateArray()
            .ToDictionary(
                hit => hit.GetProperty("_id").GetString()!,
                hit => hit.GetProperty("_source").GetProperty("content_hash").GetString()!);
    }

    public async Task IndexAsync(IReadOnlyCollection<Speech> speeches, CancellationToken cancellationToken)
    {
        foreach (var batch in speeches.Chunk(BulkBatchSize))
        {
            var response = await SendAsync(HttpMethod.Post, "_bulk", BulkBody(batch), NdjsonContentType, cancellationToken);
            ThrowOnFailedItems(response);
        }
    }

    private string BulkBody(IEnumerable<Speech> speeches)
    {
        var body = new StringBuilder();
        foreach (var speech in speeches)
        {
            var action = new { index = new { _index = names.Index, _id = speech.Id } };
            body.Append(JsonSerializer.Serialize(action)).Append('\n');
            body.Append(JsonSerializer.Serialize(speech, SpeechJson.Options)).Append('\n');
        }

        return body.ToString();
    }

    private static void ThrowOnFailedItems(string bulkResponse)
    {
        using var document = JsonDocument.Parse(bulkResponse);
        if (!document.RootElement.GetProperty("errors").GetBoolean())
        {
            return;
        }

        var failures = document.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("index"))
            .Where(result => result.TryGetProperty("error", out _))
            .Select(result => $"{result.GetProperty("_id").GetString()}: {result.GetProperty("error").GetRawText()}")
            .ToList();

        throw new EsException(
            $"{failures.Count} speeches were rejected. First failures: {string.Join("; ", failures.Take(MaxReportedBulkFailures))}");
    }

    private async Task<bool> IndexExistsAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, names.Index);
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task<string> SendAsync(
        HttpMethod method, string path, string? body, string contentType, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : new StringContent(body, Encoding.UTF8, contentType),
        };
        using var response = await http.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return response.IsSuccessStatusCode
            ? content
            : throw new EsException($"{method} {path} failed with {(int)response.StatusCode}: {content}");
    }
}
