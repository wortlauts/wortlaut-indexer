using System.Xml.Linq;
using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Parsing;

internal static class SpeakerReader
{
    private const char IdSeparator = ' ';

    private static readonly string[] NameParts = ["titel", "vorname", "namenszusatz", "nachname"];

    public static bool IsAnnouncement(XElement element) =>
        element.Name == "p" && (string?)element.Attribute("klasse") == "redner";

    public static string? AnnouncedSpeakerId(XElement announcement) =>
        RawSpeakerId(announcement)?.Split(IdSeparator)[0];

    public static Speaker? ReadAnnouncedSpeaker(XElement announcement)
    {
        var id = AnnouncedSpeakerId(announcement);
        var name = announcement.Element("redner")?.Element("name");
        if (id is null || name is null)
        {
            return null;
        }

        var recordIsMerged = RawSpeakerId(announcement)!.Contains(IdSeparator);
        return new Speaker(
            Id: id,
            Name: FullName(name, recordIsMerged),
            Faction: FactionNormalizer.Normalize(FieldText(name.Element("fraktion"), recordIsMerged)),
            Role: XmlText.ValueOrNull(name.Element("rolle")?.Element("rolle_kurz")));
    }

    private static string? RawSpeakerId(XElement announcement) =>
        (string?)announcement.Element("redner")?.Attribute("id");

    private static string FullName(XElement name, bool recordIsMerged) =>
        string.Join(' ', NameParts
            .Select(part => FieldText(name.Element(part), recordIsMerged))
            .OfType<string>());

    // The Bundestag data contains records merged from two persons: they carry several ids and repeat every field.
    private static string? FieldText(XElement? field, bool recordIsMerged)
    {
        var text = XmlText.ValueOrNull(field);
        return recordIsMerged && text is not null ? CollapseRepetition(text) : text;
    }

    private static string CollapseRepetition(string text)
    {
        var half = text.Length / 2;
        var isRepeated = text.Length % 2 == 0 && text[..half] == text[half..];
        return isRepeated ? text[..half] : text;
    }
}
