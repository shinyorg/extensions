using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.Stores.Tests;


public partial class StoreTests(ITestOutputHelper output) : IDisposable
{
    IKeyValueStore? currentStore;


    internal static DefaultJsonSerializer CreateSerializer()
    {
        var s = new DefaultJsonSerializer();
        s.Options.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
        return s;
    }


    public static IEnumerable<object[]> Data
    {
        get
        {
            var serializer = CreateSerializer();
#if ANDROID
            yield return [new SecureKeyValueStore(serializer)];
            yield return [new SettingsKeyValueStore(serializer)];
#elif IOS || MACCATALYST || __MACOS__
            yield return [new SecureKeyValueStore(serializer)];
            yield return [new SettingsKeyValueStore(serializer)];
#endif
            yield return [new MemoryKeyValueStore()];

            var file = Path.Combine(Path.GetTempPath(), "shiny-filestore-tests", Guid.NewGuid() + ".json");
            yield return [new FileKeyValueStore(file, serializer)];
        }
    }


    public void Dispose() => this.currentStore?.Clear();
}
