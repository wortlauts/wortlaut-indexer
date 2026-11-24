using Wortlaut.Indexer.Model;
using Wortlaut.Indexer.Parsing;

namespace Wortlaut.Indexer.Tests.Parsing;

public class ReactionParserTests
{
    [Theory]
    [InlineData("(Beifall bei der CDU/CSU)", "CDU/CSU")]
    [InlineData("(Beifall beim BÜNDNIS 90/DIE GRÜNEN)", "BÜNDNIS 90/DIE GRÜNEN")]
    [InlineData("(Lebhafter Beifall bei der SPD)", "SPD")]
    [InlineData("(Lang anhaltender Beifall bei der CDU/CSU)", "CDU/CSU")]
    [InlineData("(Beifall bei der CDU/CSU sowie bei Abgeordneten der SPD)", "CDU/CSU sowie bei Abgeordneten der SPD")]
    [InlineData("(Beifall der Abg. Rasha Nasr [SPD])", "Rasha Nasr [SPD]")]
    [InlineData("(Beifall)", null)]
    public void Applause_IsRecognisedWithItsSource(string comment, string? expectedSource)
    {
        var reaction = Assert.Single(ReactionParser.Parse(comment));

        Assert.Equal(ReactionType.Applause, reaction.Type);
        Assert.Equal(expectedSource, reaction.Source);
        Assert.Null(reaction.Text);
    }

    [Theory]
    [InlineData("(Heiterkeit beim BÜNDNIS 90/DIE GRÜNEN)", "BÜNDNIS 90/DIE GRÜNEN")]
    [InlineData("(Lachen bei Abgeordneten der Linken)", "Abgeordneten der Linken")]
    [InlineData("(Heiterkeit der Abg. Dr. Anja Weisgerber [CDU/CSU])", "Dr. Anja Weisgerber [CDU/CSU]")]
    [InlineData("(Große Heiterkeit)", null)]
    public void Laughter_IsRecognisedWithItsSource(string comment, string? expectedSource)
    {
        var reaction = Assert.Single(ReactionParser.Parse(comment));

        Assert.Equal(ReactionType.Laughter, reaction.Type);
        Assert.Equal(expectedSource, reaction.Source);
    }

    [Theory]
    [InlineData("(Zuruf der Abg. Heidi Reichinnek [Die Linke])", "Heidi Reichinnek [Die Linke]")]
    [InlineData("(Zurufe vom BÜNDNIS 90/DIE GRÜNEN)", "BÜNDNIS 90/DIE GRÜNEN")]
    [InlineData("(Zurufe von der SPD und der LINKEN)", "SPD und der LINKEN")]
    public void UnnamedHeckle_HasASourceButNoText(string comment, string expectedSource)
    {
        var reaction = Assert.Single(ReactionParser.Parse(comment));

        Assert.Equal(ReactionType.Heckle, reaction.Type);
        Assert.Equal(expectedSource, reaction.Source);
        Assert.Null(reaction.Text);
    }

    [Fact]
    public void UnnamedHeckleWithWords_SeparatesSourceFromText()
    {
        var reaction = Assert.Single(ReactionParser.Parse(
            "(Zurufe von der SPD und der LINKEN: Sind wir hier in der Schule, oder wie?)"));

        Assert.Equal(ReactionType.Heckle, reaction.Type);
        Assert.Equal("SPD und der LINKEN", reaction.Source);
        Assert.Equal("Sind wir hier in der Schule, oder wie?", reaction.Text);
    }

    [Fact]
    public void Objection_IsRecognisedWithItsSource()
    {
        var reaction = Assert.Single(ReactionParser.Parse("(Widerspruch bei der AfD)"));

        Assert.Equal(ReactionType.Objection, reaction.Type);
        Assert.Equal("AfD", reaction.Source);
    }

    [Theory]
    [InlineData("(Nicole Gohlke [Die Linke]: Eine linke Schulreform!)", "Nicole Gohlke [Die Linke]", "Eine linke Schulreform!")]
    [InlineData("(Dr. Franziska Brantner [BÜNDNIS 90/DIE GRÜNEN]: Unglaublich!)", "Dr. Franziska Brantner [BÜNDNIS 90/DIE GRÜNEN]", "Unglaublich!")]
    [InlineData("(Carsten Schneider [Erfurt] [SPD]: Ja!)", "Carsten Schneider [Erfurt] [SPD]", "Ja!")]
    [InlineData("(Enrico Komning [AfD], an die AfD gewandt: Ist das jetzt ihre Abschlussrede?)", "Enrico Komning [AfD]", "Ist das jetzt ihre Abschlussrede?")]
    public void NamedInterjection_IsAHeckleWithSpeakerAndText(string comment, string expectedSource, string expectedText)
    {
        var reaction = Assert.Single(ReactionParser.Parse(comment));

        Assert.Equal(ReactionType.Heckle, reaction.Type);
        Assert.Equal(expectedSource, reaction.Source);
        Assert.Equal(expectedText, reaction.Text);
    }

    [Fact]
    public void CombinedComment_IsSplitIntoOneReactionPerPart()
    {
        var reactions = ReactionParser.Parse(
            "(Beifall bei der FDP sowie bei Abgeordneten der AfD – Friedrich Merz [CDU/CSU]: Herr Vogel!)");

        Assert.Collection(
            reactions,
            applause =>
            {
                Assert.Equal(ReactionType.Applause, applause.Type);
                Assert.Equal("FDP sowie bei Abgeordneten der AfD", applause.Source);
            },
            heckle =>
            {
                Assert.Equal(ReactionType.Heckle, heckle.Type);
                Assert.Equal("Friedrich Merz [CDU/CSU]", heckle.Source);
                Assert.Equal("Herr Vogel!", heckle.Text);
            });
    }

    [Fact]
    public void DashInsideAnInterjection_StaysPartOfItsText()
    {
        var reaction = Assert.Single(ReactionParser.Parse(
            "(Martin Sichert [AfD]: Das ist – wie soll ich sagen – falsch!)"));

        Assert.Equal("Das ist – wie soll ich sagen – falsch!", reaction.Text);
    }

    [Theory]
    [InlineData("(Abg. Kay Gottschalk [AfD] meldet sich zu einer Zwischenfrage)")]
    [InlineData("(Die Abgeordneten der CDU/CSU erheben sich)")]
    [InlineData("(Unruhe)")]
    [InlineData("")]
    public void StageDirections_AreNotReactions(string comment)
    {
        Assert.Empty(ReactionParser.Parse(comment));
    }

    [Fact]
    public void Whitespace_IsNormalised()
    {
        var reaction = Assert.Single(ReactionParser.Parse("(Beifall\n   bei der SPD)"));

        Assert.Equal("SPD", reaction.Source);
    }
}
