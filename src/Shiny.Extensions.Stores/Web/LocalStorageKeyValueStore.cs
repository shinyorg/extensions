using Microsoft.JSInterop;

namespace Shiny.Extensions.Stores.Web;


/// <summary>
/// <see cref="IKeyValueStore"/> implementation backed by the browser's <c>window.localStorage</c>.
/// Blazor WebAssembly only - requires <see cref="IJSInProcessRuntime"/>.
/// </summary>
public class LocalStorageKeyValueStore(IJSRuntime jsRuntime, ISerializer serializer) : IKeyValueStore
{
    const string KeyPrefix = "shiny:kvs:settings:";
    readonly LocalStorage storage = new(jsRuntime);


    public bool IsReadOnly => false;


    public bool Contains(string key)
        => this.storage.ContainsKey(this.Format(key));


    public T? Get<T>(string key)
    {
        var json = this.storage.GetItem(this.Format(key));
        if (json == null)
            return default;

        return serializer.Deserialize<T>(json);
    }


    public void Set<T>(string key, T value)
    {
        if (value is null)
        {
            this.Remove(key);
            return;
        }

        var json = serializer.Serialize<T>(value);
        this.storage.SetItem(this.Format(key), json);
    }


    public bool Remove(string key)
        => this.storage.RemoveItem(this.Format(key));


    public void Clear()
        => this.storage.RemoveKeys(KeyPrefix);


    string Format(string key) => $"{KeyPrefix}{key}";
}
