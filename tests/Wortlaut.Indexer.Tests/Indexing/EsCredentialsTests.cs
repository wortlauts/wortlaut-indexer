using Wortlaut.Indexer.Indexing;

namespace Wortlaut.Indexer.Tests.Indexing;

public class EsCredentialsTests
{
    private static readonly TimeSpan AnyTimeout = TimeSpan.FromSeconds(5);
    private static readonly Uri AnyAddress = new("http://localhost:9200");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void WithoutUsername_NoCredentialsAreUsed(string? username)
    {
        Assert.Null(EsCredentials.From(username, "secret"));
    }

    [Fact]
    public void UsernameWithoutPassword_IsRejected()
    {
        var error = Assert.Throws<EsException>(() => EsCredentials.From("indexer", null));

        Assert.Contains("password", error.Message);
    }

    [Fact]
    public void Credentials_AreSentAsBasicAuthorization()
    {
        var header = new EsCredentials("indexer", "secret").ToAuthorizationHeader();

        Assert.Equal("Basic", header.Scheme);
        Assert.Equal("aW5kZXhlcjpzZWNyZXQ=", header.Parameter);
    }

    [Fact]
    public void ClientWithCredentials_SendsThemWithEveryRequest()
    {
        using var http = EsHttpClient.Create(AnyAddress, new EsCredentials("indexer", "secret"), AnyTimeout);

        Assert.Equal("Basic", http.DefaultRequestHeaders.Authorization?.Scheme);
    }

    [Fact]
    public void ClientWithoutCredentials_SendsNoAuthorization()
    {
        using var http = EsHttpClient.Create(AnyAddress, credentials: null, AnyTimeout);

        Assert.Null(http.DefaultRequestHeaders.Authorization);
    }
}
