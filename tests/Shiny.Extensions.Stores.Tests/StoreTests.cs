
namespace Shiny.Extensions.Stores.Tests;


public partial class StoreTests(ITestOutputHelper output) : IDisposable
{
    IKeyValueStore? currentStore;


    public static IEnumerable<object[]> Data
    {
        get
        {
            var serializer = new DefaultSerializer();
#if ANDROID
            yield return [new SecureKeyValueStore(null!, serializer)];
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