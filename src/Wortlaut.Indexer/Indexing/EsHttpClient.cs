namespace Wortlaut.Indexer.Indexing;

public static class EsHttpClient
{
    public static HttpClient Create(Uri baseAddress, EsCredentials? credentials, TimeSpan timeout)
    {
        var http = new HttpClient { BaseAddress = baseAddress, Timeout = timeout };
        http.DefaultRequestHeaders.Authorization = credentials?.ToAuthorizationHeader();
        return http;
    }
}
