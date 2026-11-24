using System.Xml;
using System.Xml.Linq;
using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Parsing;

public static class ProtocolParser
{
    public static ParsedProtocol Parse(XDocument document)
    {
        var protocol = document.Root
            ?? throw new ProtocolFormatException("The document has no root element.");
        var header = ProtocolHeader.From(protocol);

        var speeches = protocol.Descendants("rede")
            .Select(rede => SpeechParser.Parse(rede, header))
            .OfType<Speech>()
            .ToList();

        return new ParsedProtocol(header, speeches);
    }

    public static ParsedProtocol ParseFile(string path)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(path, settings);
        return Parse(XDocument.Load(reader));
    }
}
