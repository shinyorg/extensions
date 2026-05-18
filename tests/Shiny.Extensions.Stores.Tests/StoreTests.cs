using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.Stores.Tests;


public partial class StoreTests(ITestOutputHelper output) : IDisposable
{
    IKeyValueStore? currentStore;


    internal static DefaultSerializer CreateSerializer()
    {
        var s = new DefaultSerializer();
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
#elif IOS || MACCATALYST
            yield return [new SecureKeyValueStore(serializer)];
            yield return [new SettingsKeyValueStore(serializer)];
#endif
            yield return [new MemoryKeyValueStore()];
        }
    }


    public void Dispose() => this.currentStore?.Clear();
}
