using Microsoft.JSInterop;

namespace Shiny.Extensions.Stores.Web;


/// <summary>
/// <see cref="IKeyValueStore"/> implementation backed by the browser's <c>window.localStorage</c>.
/// Requires <c>_content/Shiny.Extensions.Stores.Web/shiny-storage.js</c> to be loaded via a
/// &lt;script&gt; tag in index.html before Shiny services are used.
/// </summary>
public class LocalStorageKeyValueStore(IJSRuntime jsRuntime, ISerializer serializer) : IKeyValueStore
{
    const string KeyPrefix = "shiny:kvs:settings:";
    readonly IJSInProcessRuntime js = (IJSInProcessRuntime)jsRuntime;


    public bool IsReadOnly => false;


    public bool Contains(string key)
        => this.js.Invoke<bool>("shinyLocalStorage.containsKey", this.Format(key));


    public T? Get<T>(string key)
    {
        var json = this.js.Invoke<string?>("shinyLocalStorage.getItem", this.Format(key));
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
        this.js.InvokeVoid("shinyLocalStorage.setItem", this.Format(key), json);
    }


    public bool Remove(string key)
        => this.js.Invoke<bool>("shinyLocalStorage.removeItem", this.Format(key));


    public void Clear()
        => this.js.Invoke<int>("shinyLocalStorage.removeKeys", KeyPrefix);


    string Format(string key) => $"{KeyPrefix}{key}";
}
