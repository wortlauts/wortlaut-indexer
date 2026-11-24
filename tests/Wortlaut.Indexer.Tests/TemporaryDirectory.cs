namespace Wortlaut.Indexer.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wortlaut-tests-{Guid.NewGuid():N}");

    public TemporaryDirectory() => Directory.CreateDirectory(Path);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
