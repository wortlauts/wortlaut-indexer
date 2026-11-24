using System.Xml.Linq;
using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Tests.Parsing;

public class AgendaItemReaderTests
{
    [Fact]
    public void BoldTitles_AreTheAgendaItem()
    {
        var topic = XElement.Parse("""
            <tagesordnungspunkt top-id="Tagesordnungspunkt 6">
              <p klasse="J">Ich rufe auf die Tagesordnungspunkte 6a und 6b:</p>
              <p klasse="T_NaS">a) Beratung des Antrags der AfD</p>
              <p klasse="T_fett">Stopp der Deindustrialisierung</p>
              <p klasse="T_NaS">b) Beratung des Antrags der AfD</p>
              <p klasse="T_fett">Insolvenzwelle stoppen</p>
            </tagesordnungspunkt>
            """);

        Assert.Equal("Stopp der Deindustrialisierung / Insolvenzwelle stoppen", AgendaItemReader.Read(topic));
    }

    [Fact]
    public void WithoutBoldTitle_TheFirstDescriptionIsUsedWithoutItsListMarker()
    {
        var topic = XElement.Parse("""
            <tagesordnungspunkt top-id="Tagesordnungspunkt 5">
              <p klasse="T_NaS">– Zweite und dritte Beratung des Entwurfs eines Gesetzes</p>
              <p klasse="T_Drs">Drucksachen 21/8239</p>
            </tagesordnungspunkt>
            """);

        Assert.Equal("Zweite und dritte Beratung des Entwurfs eines Gesetzes", AgendaItemReader.Read(topic));
    }

    [Fact]
    public void WithoutAnyTitle_TheTopicIdIsUsed()
    {
        var topic = XElement.Parse("""<tagesordnungspunkt top-id="Zusatzpunkt 3"><p klasse="J">Text</p></tagesordnungspunkt>""");

        Assert.Equal("Zusatzpunkt 3", AgendaItemReader.Read(topic));
    }

    [Fact]
    public void SpeechOutsideAnyTopic_HasNoAgendaItem()
    {
        Assert.Null(AgendaItemReader.Read(null));
    }
}
