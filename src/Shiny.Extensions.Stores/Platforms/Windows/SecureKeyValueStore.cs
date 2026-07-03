using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography.DataProtection;

namespace Shiny.Extensions.Stores;


public class SecureKeyValueStore : IKeyValueStore
{
    readonly IKeyValueStore settingsStore;
    readonly ISerializer serializer;


    /// <summary>
    /// Creates a DPAPI-encrypted secure store. By default it is backed by the native
    /// ApplicationData settings container (packaged apps). Pass <paramref name="backingStore"/> to
    /// persist elsewhere — e.g. a <see cref="FileKeyValueStore"/> for unpackaged desktop apps, which
    /// still get DPAPI encryption layered over the file.
    /// </summary>
    public SecureKeyValueStore(ISerializer serializer, IKeyValueStore? backingStore = null)
    {
        this.serializer = serializer;
        this.settingsStore = backingStore ?? new SettingsKeyValueStore(serializer) { ContainerName = "ShinySecure" };
    }


    public bool IsReadOnly => false;
    public void Clear() => this.settingsStore.Clear();
    public bool Contains(string key) => this.settingsStore.Contains(key);


    public T? Get<T>(string key)
    {
        var data = this.settingsStore.Get<byte[]>(key);
        if (data == null)
            return default;

        var provider = new DataProtectionProvider();
        var buffer = provider.UnprotectAsync(data.AsBuffer()).GetResults();
        var json = Encoding.UTF8.GetString(buffer.ToArray());
        return this.serializer.Deserialize<T>(json);
    }


    public bool Remove(string key) => this.settingsStore.Remove(key);


    public void Set<T>(string key, T value)
    {
        if (value is null)
        {
            this.Remove(key);
            return;
        }

        var json = this.serializer.Serialize<T>(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        // LOCAL=user and LOCAL=machine do not require enterprise auth capability
        var provider = new DataProtectionProvider("LOCAL=user");
        var buffer = provider.ProtectAsync(bytes.AsBuffer()).GetResults();
        this.settingsStore.Set(key, buffer.ToArray());
    }
}
