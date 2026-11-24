using System.Text.Json;
using System.Text.Json.Nodes;
using Wortlaut.Indexer.Indexing;
using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Tests.Indexing;

public class IndexTemplateTests
{
    [Fact]
    public void EveryFieldOfASerialisedSpeech_IsMappedInTheTemplate()
    {
        var speech = ContentHash.Seal(TestSpeeches.Create());
        var document = JsonSerializer.SerializeToNode(speech, SpeechJson.Options)!.AsObject();
        var mappedProperties = JsonNode.Parse(IndexTemplate.Json)!["template"]!["mappings"]!["properties"]!.AsObject();

        var unmapped = UnmappedPaths(document, mappedProperties, prefix: string.Empty).ToList();

        Assert.Empty(unmapped);
    }

    [Fact]
    public void TheTemplate_RejectsUnknownFields()
    {
        var mappings = JsonNode.Parse(IndexTemplate.Json)!["template"]!["mappings"]!;

        Assert.Equal("strict", mappings["dynamic"]!.GetValue<string>());
    }

    private static IEnumerable<string> UnmappedPaths(JsonObject document, JsonObject mappedProperties, string prefix)
    {
        foreach (var (name, value) in document)
        {
            var path = prefix + name;
            if (mappedProperties[name] is not JsonObject mapping)
            {
                yield return path;
                continue;
            }

            var nested = value switch
            {
                JsonObject obj => [obj],
                JsonArray array => array.OfType<JsonObject>().ToList(),
                _ => new List<JsonObject>(),
            };

            foreach (var child in nested)
            {
                var childProperties = mapping["properties"]?.AsObject() ?? [];
                foreach (var unmapped in UnmappedPaths(child, childProperties, path + "."))
                {
                    yield return unmapped;
                }
            }
        }
    }
}
