using Wortlaut.Indexer.Sources;

namespace Wortlaut.Indexer.Tests.Sources;

public class ProtocolListingParserTests
{
    [Fact]
    public void ProtocolLinks_AreExtractedOncePerFileInPageOrder()
    {
        const string page = """
            <li><a href="https://www.bundestag.de/resource/blob/1233202/21108.xml" title="Plenarprotokoll der 108. Sitzung">XML</a>
                <a href="https://www.bundestag.de/resource/blob/1233202/21108.xml">Download</a></li>
            <li><a href="https://www.bundestag.de/resource/blob/1233302/21109.xml">XML</a></li>
            <li><a href="https://www.bundestag.de/resource/blob/999/other.pdf">PDF</a></li>
            """;

        var urls = ProtocolListingParser.ProtocolUrls(page);

        Assert.Equal(
            ["https://www.bundestag.de/resource/blob/1233202/21108.xml", "https://www.bundestag.de/resource/blob/1233302/21109.xml"],
            urls.Select(url => url.ToString()));
    }

    [Fact]
    public void PageWithoutProtocols_YieldsNoLinks()
    {
        Assert.Empty(ProtocolListingParser.ProtocolUrls("<p>Inhalte werden geladen</p>"));
    }

    [Fact]
    public void FileName_IsTheLastSegmentOfTheUrl()
    {
        var url = new Uri("https://www.bundestag.de/resource/blob/1233202/21108.xml");

        Assert.Equal("21108.xml", ProtocolListingParser.FileName(url));
    }
}
