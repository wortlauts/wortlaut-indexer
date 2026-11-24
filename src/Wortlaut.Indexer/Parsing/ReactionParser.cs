using System.Text.RegularExpressions;
using Wortlaut.Indexer.Model;

namespace Wortlaut.Indexer.Parsing;

internal static partial class ReactionParser
{
    private const string PartSeparator = " – ";

    private static readonly (Regex Pattern, ReactionType Type)[] KeywordReactions =
    [
        (new(@"^(?:\p{L}+\s+){0,2}?(?:Beifall|Händeklatschen)\b", RegexOptions.CultureInvariant), ReactionType.Applause),
        (new(@"^(?:\p{L}+\s+)?(?:Lachen|Heiterkeit|Gelächter)\b", RegexOptions.CultureInvariant), ReactionType.Laughter),
        (new(@"^Zurufe?\b", RegexOptions.CultureInvariant), ReactionType.Heckle),
        (new(@"^Widerspruch\b", RegexOptions.CultureInvariant), ReactionType.Objection),
    ];

    public static IReadOnlyList<Reaction> Parse(string comment)
    {
        var reactions = new List<Reaction>();
        foreach (var part in SplitIntoParts(comment))
        {
            var reaction = ParsePart(part);
            if (reaction is not null)
            {
                reactions.Add(reaction);
            }
            else if (reactions.LastOrDefault() is { Type: ReactionType.Heckle } heckle)
            {
                reactions[^1] = heckle with { Text = $"{heckle.Text}{PartSeparator}{part}" };
            }
        }

        return reactions;
    }

    private static IEnumerable<string> SplitIntoParts(string comment)
    {
        var normalized = TextNormalizer.Normalize(comment);
        var unwrapped = normalized.StartsWith('(') && normalized.EndsWith(')') ? normalized[1..^1] : normalized;
        return unwrapped.Split(PartSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static Reaction? ParsePart(string part) => ParseKeywordReaction(part) ?? ParseNamedInterjection(part);

    private static Reaction? ParseKeywordReaction(string part)
    {
        foreach (var (pattern, type) in KeywordReactions)
        {
            var match = pattern.Match(part);
            if (match.Success)
            {
                var (source, text) = SplitSourceAndText(part[match.Length..]);
                return new Reaction(type, source, text);
            }
        }

        return null;
    }

    private static Reaction? ParseNamedInterjection(string part)
    {
        var match = NamedInterjection().Match(part);
        return match.Success
            ? new Reaction(ReactionType.Heckle, match.Groups["source"].Value, match.Groups["text"].Value)
            : null;
    }

    private static (string? Source, string? Text) SplitSourceAndText(string remainder)
    {
        var colon = remainder.IndexOf(':');
        return colon < 0
            ? (SourceOf(remainder), null)
            : (SourceOf(remainder[..colon]), NullIfEmpty(remainder[(colon + 1)..]));
    }

    private static string? SourceOf(string phrase) =>
        NullIfEmpty(LeadingPreposition().Replace(phrase.Trim(), string.Empty));

    private static string? NullIfEmpty(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    [GeneratedRegex(@"^(?<source>.+?\[[^\]]+\])(?:,[^:]*)?:\s*(?<text>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex NamedInterjection();

    [GeneratedRegex(@"^(?:(?:bei|beim|von|vom|vonseiten|der|des|dem|den)\s+)+(?:Abg\.\s+)?", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingPreposition();
}
