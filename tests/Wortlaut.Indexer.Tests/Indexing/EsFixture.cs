using System.Net.Http.Headers;
using System.Text;
using Testcontainers.Elasticsearch;

namespace Wortlaut.Indexer.Tests.Indexing;

public sealed class EsFixture : IAsyncLifetime
{
    private const string Image = "docker.elastic.co/elasticsearch/elasticsearch:9.5.7";

    private readonly ElasticsearchContainer container = new ElasticsearchBuilder(Image).Build();

    public async ValueTask InitializeAsync() => await container.StartAsync();

    public async ValueTask DisposeAsync() => await container.DisposeAsync();

    public HttpClient CreateClient()
    {
        var connection = new Uri(container.GetConnectionString());
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(connection.UserInfo));
        var selfSignedCertificateOfTheTestContainer = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        var client = new HttpClient(selfSignedCertificateOfTheTestContainer)
        {
            BaseAddress = new Uri(connection.GetLeftPart(UriPartial.Authority)),
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return client;
    }
}
