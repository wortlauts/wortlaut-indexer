using System.Xml.Linq;
using Wortlaut.Indexer.Model;
using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Tests.Parsing;

public class ProtocolParserTests
{
    [Theory]
    [InlineData(SampleProtocols.Period21, 21, 108, "2026-11-12")]
    [InlineData(SampleProtocols.Period20, 20, 214, "2025-03-18")]
    [InlineData(SampleProtocols.Period19, 19, 239, "2021-09-07")]
    public void Header_IsReadFromTheProtocolAttributes(string file, int period, int session, string date)
    {
        var protocol = ProtocolParser.ParseFile(SampleProtocols.PathOf(file));

        Assert.Equal(new ProtocolHeader(period, session, DateOnly.Parse(date)), protocol.Header);
    }

    [Theory]
    [InlineData(SampleProtocols.Period21)]
    [InlineData(SampleProtocols.Period20)]
    [InlineData(SampleProtocols.Period19)]
    public void EverySampleSpeech_HasATextASpeakerAndReactions(string file)
    {
        var protocol = ProtocolParser.ParseFile(SampleProtocols.PathOf(file));

        Assert.Equal(3, protocol.Speeches.Count);
        Assert.All(protocol.Speeches, speech =>
        {
            Assert.False(string.IsNullOrWhiteSpace(speech.Text));
            Assert.False(string.IsNullOrWhiteSpace(speech.Speaker.Name));
            Assert.NotEmpty(speech.Reactions);
            Assert.True(speech.WordCount > 100);
        });
    }

    [Fact]
    public void SpeechInterruptedByThePresident_ExcludesThePresidentsWords()
    {
        var protocol = ProtocolParser.ParseFile(SampleProtocols.PathOf(SampleProtocols.Period21));

        var speech = Assert.Single(protocol.Speeches, candidate => candidate.Id == "21-108-01000");

        Assert.Equal("Maren Kaminski", speech.Speaker.Name);
        Assert.Equal("Die Linke", speech.Speaker.Faction);
        Assert.DoesNotContain("hat nun die Parlamentarische Staatssekretärin", speech.Text);
    }

    [Fact]
    public void NamedInterjection_ArrivesAsAHeckleWithTheInterjectorsFaction()
    {
        var protocol = ProtocolParser.ParseFile(SampleProtocols.PathOf(SampleProtocols.Period21));

        var speech = Assert.Single(protocol.Speeches, candidate => candidate.Id == "21-108-01000");

        Assert.Contains(
            speech.Reactions,
            reaction => reaction is { Type: ReactionType.Heckle, Source: "Ulle Schauws [BÜNDNIS 90/DIE GRÜNEN]", Text: "Nein, wir auch nicht!" });
    }

    [Fact]
    public void Speeches_AreOrderedLikeInTheProtocol()
    {
        var protocol = ProtocolParser.ParseFile(SampleProtocols.PathOf(SampleProtocols.Period20));

        Assert.Equal(["20-214-00100", "20-214-00300", "20-214-00600"], protocol.Speeches.Select(speech => speech.Id));
    }

    [Fact]
    public void DocumentWithoutRoot_IsRejected()
    {
        Assert.Throws<ProtocolFormatException>(() => ProtocolParser.Parse(new XDocument()));
    }

    [Fact]
    public void ProtocolWithMalformedDate_IsRejected()
    {
        var document = XDocument.Parse(
            """<dbtplenarprotokoll wahlperiode="21" sitzung-nr="1" sitzung-datum="2026-11-12"><sitzungsverlauf/></dbtplenarprotokoll>""");

        var exception = Assert.Throws<ProtocolFormatException>(() => ProtocolParser.Parse(document));

        Assert.Contains("sitzung-datum", exception.Message);
    }

    [Fact]
    public void ProtocolWithoutSessionNumber_IsRejected()
    {
        var document = XDocument.Parse(
            """<dbtplenarprotokoll wahlperiode="21" sitzung-datum="12.11.2026"><sitzungsverlauf/></dbtplenarprotokoll>""");

        var exception = Assert.Throws<ProtocolFormatException>(() => ProtocolParser.Parse(document));

        Assert.Contains("sitzung-nr", exception.Message);
    }
}
