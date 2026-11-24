using System.Text.RegularExpressions;
using System.Xml.Linq;
using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Parsing;

internal static partial class SpeechParser
{
    private const string ParagraphSeparator = "\n";

    public static Speech? Parse(XElement rede, ProtocolHeader header)
    {
        var speaker = rede.Elements().Where(SpeakerReader.IsAnnouncement)
            .Select(SpeakerReader.ReadAnnouncedSpeaker)
            .FirstOrDefault(announced => announced is not null);
        if (speaker is null)
        {
            return null;
        }

        var content = SpeechContent.Collect(rede, speaker.Id);
        if (content.Text.Length == 0)
        {
            return null;
        }

        return new Speech(
            Id: $"{header.LegislativePeriod}-{header.Session}-{RunningNumber(rede)}",
            LegislativePeriod: header.LegislativePeriod,
            Session: header.Session,
            Date: header.Date,
            AgendaItem: AgendaItemReader.Read(rede.Ancestors("tagesordnungspunkt").FirstOrDefault()),
            Delivery: rede.Ancestors("anlagen").Any() ? SpeechDelivery.WrittenStatement : SpeechDelivery.Plenary,
            Speaker: speaker,
            Text: content.Text,
            Reactions: content.Reactions,
            WordCount: content.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
            SourceUrl: header.SourceUrl,
            Rights: Rights.OfficialWork);
    }

    private static string RunningNumber(XElement rede)
    {
        var id = (string?)rede.Attribute("id")
            ?? throw new ProtocolFormatException("A <rede> element has no id.");
        var number = RunningNumberSuffix().Match(id);
        return number.Success ? number.Value : throw new ProtocolFormatException($"The speech id '{id}' has no running number.");
    }

    [GeneratedRegex(@"\d{5}$")]
    private static partial Regex RunningNumberSuffix();

    private sealed record SpeechContent(string Text, IReadOnlyList<Reaction> Reactions)
    {
        public static SpeechContent Collect(XElement rede, string speakerId)
        {
            var paragraphs = new List<string>();
            var reactions = new List<Reaction>();
            var speakerIsTalking = false;

            foreach (var element in rede.Elements())
            {
                if (SpeakerReader.IsAnnouncement(element))
                {
                    speakerIsTalking = SpeakerReader.AnnouncedSpeakerId(element) == speakerId;
                }
                else if (element.Name == "name")
                {
                    speakerIsTalking = false;
                }
                else if (speakerIsTalking && element.Name == "kommentar")
                {
                    reactions.AddRange(ReactionParser.Parse(XmlText.PlainText(element)));
                }
                else if (speakerIsTalking && element.Name is { LocalName: "p" or "zitat" })
                {
                    paragraphs.Add(XmlText.PlainText(element));
                }
            }

            return new SpeechContent(
                string.Join(ParagraphSeparator, paragraphs.Where(paragraph => paragraph.Length > 0)),
                reactions);
        }
    }
}
