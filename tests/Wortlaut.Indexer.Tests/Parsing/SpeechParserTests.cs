using System.Xml.Linq;
using Wortlaut.Indexer.Model;
using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Tests.Parsing;

public class SpeechParserTests
{
    private static readonly ProtocolHeader Header = new(21, 108, new DateOnly(2026, 11, 12));

    private const string MainSpeakerAnnouncement = """
        <p klasse="redner"><redner id="42"><name><titel>Dr.</titel><vorname>Anna</vorname><nachname>Muster</nachname><fraktion>DIE LINKE</fraktion></name></redner>Dr. Anna Muster (Die Linke):</p>
        """;

    [Fact]
    public void Speech_ContainsOnlyTheParagraphsOfItsSpeaker()
    {
        var speech = ParseInPlenum($"""
            <rede id="ID2110800600">
              {MainSpeakerAnnouncement}
              <p klasse="J_1">Erster Absatz.</p>
              <p klasse="J">Zweiter Absatz.</p>
              <name>Präsidentin Julia Klöckner:</name>
              <p klasse="J_1">Ihre Redezeit ist abgelaufen.</p>
              {MainSpeakerAnnouncement}
              <p klasse="J">Letzter Absatz.</p>
            </rede>
            """);

        Assert.Equal("Erster Absatz.\nZweiter Absatz.\nLetzter Absatz.", speech.Text);
    }

    [Fact]
    public void Reactions_AreOnlyThoseDuringTheSpeakersOwnWords()
    {
        var speech = ParseInPlenum($"""
            <rede id="ID2110800600">
              {MainSpeakerAnnouncement}
              <p klasse="J_1">Ein Satz.</p>
              <kommentar>(Beifall bei der Linken)</kommentar>
              <name>Präsidentin Julia Klöckner:</name>
              <p klasse="J_1">Ich danke Ihnen.</p>
              <kommentar>(Beifall bei der AfD)</kommentar>
            </rede>
            """);

        var reaction = Assert.Single(speech.Reactions);
        Assert.Equal(ReactionType.Applause, reaction.Type);
        Assert.Equal("Linken", reaction.Source);
    }

    [Fact]
    public void QuestionsOfOtherSpeakers_AreNotPartOfTheSpeech()
    {
        var speech = ParseInPlenum($"""
            <rede id="ID2110800600">
              {MainSpeakerAnnouncement}
              <p klasse="J_1">Antwort vor der Frage.</p>
              <name>Präsidentin Julia Klöckner:</name>
              <p klasse="J_1">Bitte, Herr Kollege.</p>
              <p klasse="redner"><redner id="99"><name><vorname>Max</vorname><nachname>Frager</nachname></name></redner>Max Frager (AfD):</p>
              <p klasse="J_1">Eine Zwischenfrage.</p>
              {MainSpeakerAnnouncement}
              <p klasse="J">Antwort nach der Frage.</p>
            </rede>
            """);

        Assert.Equal("Antwort vor der Frage.\nAntwort nach der Frage.", speech.Text);
    }

    [Fact]
    public void SpeechStartingWithThePresident_IsAttributedToTheFirstAnnouncedSpeaker()
    {
        var speech = ParseInPlenum($"""
            <rede id="ID2110800600">
              <name>Vizepräsident Wolfgang Kubicki:</name>
              <p klasse="J_1">Vielen Dank, Herr Kollege.</p>
              {MainSpeakerAnnouncement}
              <p klasse="J">Das Wort des Redners.</p>
            </rede>
            """);

        Assert.Equal("Das Wort des Redners.", speech.Text);
        Assert.Equal("42", speech.Speaker.Id);
    }

    [Fact]
    public void Footnotes_AreLeftOutOfTheText()
    {
        var speech = ParseInPlenum($"""
            <rede id="ID2110800600">
              {MainSpeakerAnnouncement}
              <p klasse="J">Die Abstimmung<fussnote>Anlagen 2 und 3</fussnote> beginnt jetzt.</p>
            </rede>
            """);

        Assert.Equal("Die Abstimmung beginnt jetzt.", speech.Text);
    }

    [Fact]
    public void QuotedParagraphs_AreKeptInTheText()
    {
        var speech = ParseInPlenum($"""
            <rede id="ID2110800600">
              {MainSpeakerAnnouncement}
              <p klasse="Z">„Energie muss bezahlbar sein.“</p>
            </rede>
            """);

        Assert.Equal("„Energie muss bezahlbar sein.“", speech.Text);
    }

    [Fact]
    public void Speaker_IsReadWithNormalisedFactionAndRole()
    {
        var speech = ParseInPlenum("""
            <rede id="ID2110800600">
              <p klasse="redner"><redner id="7"><name><vorname>Karin</vorname><nachname>Prien</nachname><fraktion>Fraktionslos</fraktion><rolle><rolle_lang>Bundesministerin für Bildung</rolle_lang><rolle_kurz>Bundesministerin BMBFSFJ</rolle_kurz></rolle></name></redner>Karin Prien:</p>
              <p klasse="J">Text.</p>
            </rede>
            """);

        Assert.Equal(new Speaker("7", "Karin Prien", "fraktionslos", "Bundesministerin BMBFSFJ"), speech.Speaker);
    }

    [Fact]
    public void Speaker_NameIncludesTitleAndNameAffixes()
    {
        var speech = ParseInPlenum("""
            <rede id="ID2110800600">
              <p klasse="redner"><redner id="8"><name><titel>Dr.</titel><vorname>Ursula</vorname><namenszusatz>von der</namenszusatz><nachname>Leyen</nachname><ortszusatz>Hannover</ortszusatz></name></redner></p>
              <p klasse="J">Text.</p>
            </rede>
            """);

        Assert.Equal("Dr. Ursula von der Leyen", speech.Speaker.Name);
    }

    [Fact]
    public void SpeakerRecordMergedFromTwoPersons_IsRepaired()
    {
        var speech = ParseInPlenum("""
            <rede id="ID2110800600">
              <p klasse="redner"><redner id="11005217 999990074"><name><vorname>SvenjaSvenja</vorname><nachname>SchulzeSchulze</nachname><fraktion>SPDSPD</fraktion></name></redner></p>
              <p klasse="J">Text.</p>
            </rede>
            """);

        Assert.Equal(new Speaker("11005217", "Svenja Schulze", "SPD", null), speech.Speaker);
    }

    [Fact]
    public void RepeatedNameOfAnOrdinaryRecord_IsLeftAlone()
    {
        var speech = ParseInPlenum("""
            <rede id="ID2110800600">
              <p klasse="redner"><redner id="5"><name><vorname>Bobo</vorname><nachname>Mustermann</nachname></name></redner></p>
              <p klasse="J">Text.</p>
            </rede>
            """);

        Assert.Equal("Bobo Mustermann", speech.Speaker.Name);
    }

    [Fact]
    public void Id_CombinesPeriodSessionAndRunningNumber()
    {
        var speech = ParseInPlenum($"""<rede id="ID2110800600">{MainSpeakerAnnouncement}<p klasse="J">Text.</p></rede>""");

        Assert.Equal("21-108-00600", speech.Id);
    }

    [Fact]
    public void Speech_CarriesProtocolMetadataAndRights()
    {
        var speech = ParseInPlenum($"""<rede id="ID2110800600">{MainSpeakerAnnouncement}<p klasse="J">Text.</p></rede>""");

        Assert.Equal(21, speech.LegislativePeriod);
        Assert.Equal(108, speech.Session);
        Assert.Equal(new DateOnly(2026, 11, 12), speech.Date);
        Assert.Equal("Tagesordnungspunkt 5", speech.AgendaItem);
        Assert.Equal("https://dserver.bundestag.de/btp/21/21108.pdf", speech.SourceUrl);
        Assert.Equal(Rights.OfficialWork, speech.Rights);
    }

    [Fact]
    public void WordCount_CountsTheWordsOfTheText()
    {
        var speech = ParseInPlenum($"""
            <rede id="ID2110800600">
              {MainSpeakerAnnouncement}
              <p klasse="J">Eins zwei drei.</p>
              <p klasse="J">Vier fünf.</p>
            </rede>
            """);

        Assert.Equal(5, speech.WordCount);
    }

    [Fact]
    public void SpeechInTheAppendix_IsAWrittenStatement()
    {
        var protocol = XElement.Parse($"""
            <dbtplenarprotokoll>
              <sitzungsverlauf/>
              <anlagen><anlage><anlagen-text>
                <rede id="ID2110800700">{MainSpeakerAnnouncement}<p klasse="J">Zu Protokoll gegeben.</p></rede>
              </anlagen-text></anlage></anlagen>
            </dbtplenarprotokoll>
            """);

        var speech = SpeechParser.Parse(protocol.Descendants("rede").Single(), Header);

        Assert.Equal(SpeechDelivery.WrittenStatement, speech!.Delivery);
        Assert.Null(speech.AgendaItem);
    }

    [Fact]
    public void SpeechWithoutText_IsSkipped()
    {
        var redeXml = $"""<rede id="ID2110800600">{MainSpeakerAnnouncement}<kommentar>(Beifall)</kommentar></rede>""";

        Assert.Null(TryParseInPlenum(redeXml));
    }

    [Fact]
    public void SpeechWithoutAnnouncedSpeaker_IsSkipped()
    {
        var redeXml = """<rede id="ID2110800600"><name>Präsident:</name><p klasse="J">Text.</p></rede>""";

        Assert.Null(TryParseInPlenum(redeXml));
    }

    private static Speech ParseInPlenum(string redeXml) =>
        TryParseInPlenum(redeXml) ?? throw new InvalidOperationException("The speech was skipped.");

    private static Speech? TryParseInPlenum(string redeXml)
    {
        var protocol = XElement.Parse($"""
            <dbtplenarprotokoll>
              <sitzungsverlauf>
                <tagesordnungspunkt top-id="Tagesordnungspunkt 5">{redeXml}</tagesordnungspunkt>
              </sitzungsverlauf>
              <anlagen/>
            </dbtplenarprotokoll>
            """);

        return SpeechParser.Parse(protocol.Descendants("rede").Single(), Header);
    }
}
