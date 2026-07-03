using System.IO;

namespace Shiny.Extensions.Stores.Tests;


public class FileKeyValueStoreTests : IDisposable
{
    readonly string filePath;

    public FileKeyValueStoreTests()
        => this.filePath = Path.Combine(
            Path.GetTempPath(),
            "shiny-filestore-tests",
            Guid.NewGuid() + ".json"
        );

    public void Dispose()
    {
        if (File.Exists(this.filePath))
            File.Delete(this.filePath);
    }


    FileKeyValueStore Create() => new(this.filePath, StoreTests.CreateSerializer());


    [Fact(DisplayName = "FileStore - persists across instances")]
    public void PersistsAcrossInstances()
    {
        var store = this.Create();
        store.Set("repoSort", "name");
        store.Set("pollSeconds", 30);
        store.Set("muted", true);
        store.Set("mode", MyTestEnum.Bye);

        // A brand new instance pointed at the same file is the "app restart" scenario.
        var reopened = this.Create();
        reopened.Get<string>("repoSort").ShouldBe("name");
        reopened.Get<int>("pollSeconds").ShouldBe(30);
        reopened.Get<bool>("muted").ShouldBeTrue();
        reopened.Get<MyTestEnum>("mode").ShouldBe(MyTestEnum.Bye);
    }


    [Fact(DisplayName = "FileStore - writes a file on set")]
    public void WritesFileOnSet()
    {
        File.Exists(this.filePath).ShouldBeFalse();
        this.Create().Set("k", "v");
        File.Exists(this.filePath).ShouldBeTrue();
    }


    [Fact(DisplayName = "FileStore - byte[] round-trips")]
    public void ByteArrayRoundTrips()
    {
        var bytes = new byte[] { 1, 2, 3, 250 };
        this.Create().Set("blob", bytes);
        this.Create().Get<byte[]>("blob").ShouldBe(bytes);
    }


    [Fact(DisplayName = "FileStore - remove persists")]
    public void RemovePersists()
    {
        var store = this.Create();
        store.Set("k", "v");
        store.Remove("k").ShouldBeTrue();

        this.Create().Contains("k").ShouldBeFalse();
    }


    [Fact(DisplayName = "FileStore - clear persists")]
    public void ClearPersists()
    {
        var store = this.Create();
        store.Set("a", "1");
        store.Set("b", "2");
        store.Clear();

        var reopened = this.Create();
        reopened.Contains("a").ShouldBeFalse();
        reopened.Contains("b").ShouldBeFalse();
    }


    [Fact(DisplayName = "FileStore - recovers from a corrupt file")]
    public void RecoversFromCorruptFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.filePath)!);
        File.WriteAllText(this.filePath, "{ this is not valid json ]");

        // Load must not throw; the store starts empty and the next write replaces the corrupt file.
        var store = this.Create();
        store.Contains("anything").ShouldBeFalse();

        store.Set("k", "v");
        this.Create().Get<string>("k").ShouldBe("v");
    }
}
