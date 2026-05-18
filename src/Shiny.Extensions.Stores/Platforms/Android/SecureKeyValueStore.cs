using Javax.Crypto;

namespace Shiny.Extensions.Stores;


public class SecureKeyValueStore : IKeyValueStore
{
    readonly object syncLock = new();
    readonly SettingsKeyValueStore settingsStore;
    readonly AndroidKeyStore keyStore;
    readonly ISerializer serializer;


    public SecureKeyValueStore(ISerializer serializer)
    {
        this.serializer = serializer;
        this.settingsStore = new SettingsKeyValueStore(serializer);
        this.keyStore = new AndroidKeyStore(
            Application.Context,
            this.settingsStore,
            $"{Application.Context.PackageName}.secure",
            false
        );
    }


    public bool IsReadOnly => false;


    public void Clear() => this.settingsStore.Clear();


    public bool Contains(string key) => this.settingsStore.Contains(SecureKey(key));


    public T? Get<T>(string key)
    {
        var secureKey = SecureKey(key);
        if (!this.settingsStore.Contains(secureKey))
            return default;

        var encValue = this.settingsStore.Get<string>(secureKey);
        if (encValue == null)
            return default;

        var data = Convert.FromBase64String(encValue);
        lock (this.syncLock)
        {
            try
            {
                var value = this.keyStore.Decrypt(data);
                if (value == null)
                    return default;

                return this.serializer.Deserialize<T>(value);
            }
            catch (AEADBadTagException)
            {
                // unable to decrypt due to app uninstall, removing old key
                this.Remove(key);
                return default;
            }
        }
    }


    public bool Remove(string key) => this.settingsStore.Remove(SecureKey(key));


    public void Set<T>(string key, T value)
    {
        if (value is null)
        {
            this.Remove(key);
            return;
        }

        var content = this.serializer.Serialize<T>(value);
        var data = this.keyStore.Encrypt(content);
        var encValue = Convert.ToBase64String(data);
        this.settingsStore.Set(SecureKey(key), encValue);
    }


    static string SecureKey(string key) => "sec-" + key;
}
