using Wortlaut.Indexer.Indexing;

namespace Wortlaut.Indexer.Tests.Indexing;

public class ContentHashTests
{
    [Fact]
    public void SameContent_ProducesTheSameHash()
    {
        var first = ContentHash.Seal(TestSpeeches.Create());
        var second = ContentHash.Seal(TestSpeeches.Create());

        Assert.Equal(first.ContentHash, second.ContentHash);
    }

    [Fact]
    public void ChangedText_ProducesAnotherHash()
    {
        var original = ContentHash.Seal(TestSpeeches.Create(text: "Erster Text."));
        var corrected = ContentHash.Seal(TestSpeeches.Create(text: "Korrigierter Text."));

        Assert.NotEqual(original.ContentHash, corrected.ContentHash);
    }

    [Fact]
    public void ExistingHash_DoesNotInfluenceTheNewHash()
    {
        var fresh = ContentHash.Seal(TestSpeeches.Create());
        var resealed = ContentHash.Seal(TestSpeeches.Create(contentHash: "stale"));

        Assert.Equal(fresh.ContentHash, resealed.ContentHash);
    }

    [Fact]
    public void Hash_IsALowercaseSha256Hex()
    {
        var hash = ContentHash.Seal(TestSpeeches.Create()).ContentHash;

        Assert.Matches("^[0-9a-f]{64}$", hash);
    }
}
