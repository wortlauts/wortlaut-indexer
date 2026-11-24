using System.Globalization;
using System.Xml.Linq;

namespace Wortlaut.Indexer.Parsing;

public sealed record ProtocolHeader(int LegislativePeriod, int Session, DateOnly Date)
{
    private const string DateFormat = "dd.MM.yyyy";

    public string SourceUrl =>
        $"https://dserver.bundestag.de/btp/{LegislativePeriod}/{LegislativePeriod}{Session:000}.pdf";

    internal static ProtocolHeader From(XElement protocol) => new(
        LegislativePeriod: IntegerAttribute(protocol, "wahlperiode"),
        Session: IntegerAttribute(protocol, "sitzung-nr"),
        Date: DateAttribute(protocol, "sitzung-datum"));

    private static int IntegerAttribute(XElement element, string name) =>
        int.TryParse(RequiredAttribute(element, name), CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new ProtocolFormatException($"Attribute '{name}' is not a number.");

    private static DateOnly DateAttribute(XElement element, string name) =>
        DateOnly.TryParseExact(RequiredAttribute(element, name), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : throw new ProtocolFormatException($"Attribute '{name}' is not a date in the format {DateFormat}.");

    private static string RequiredAttribute(XElement element, string name) =>
        (string?)element.Attribute(name)
        ?? throw new ProtocolFormatException($"Attribute '{name}' is missing on <{element.Name}>.");
}
