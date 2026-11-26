using Wortlaut.Indexer.Cli;

namespace Wortlaut.Indexer.Tests.Cli;

public class ImportCommandTests
{
    [Theory]
    [InlineData("http://localhost:9200")]
    [InlineData("http://elasticsearch:9200")]
    [InlineData("https://search.example.org")]
    public void ElasticsearchUrl_IsAccepted(string url)
    {
        var result = ImportCommand.Create().Parse(["--elasticsearch", url]);

        Assert.Empty(result.Errors);
        Assert.Equal(url, result.GetValue<string>("--elasticsearch"));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://localhost")]
    public void ElasticsearchUrl_ThatIsNoHttpAddress_IsRejected(string url)
    {
        var result = ImportCommand.Create().Parse(["--elasticsearch", url]);

        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void WithoutOptions_ImportsFromTheLocalDefaults()
    {
        var result = ImportCommand.Create().Parse([]);

        Assert.Empty(result.Errors);
        Assert.Equal("http://localhost:9200", result.GetValue<string>("--elasticsearch"));
    }
}
