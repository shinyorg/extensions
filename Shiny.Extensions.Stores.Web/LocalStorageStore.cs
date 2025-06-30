using System.Text.Json;
using Microsoft.JSInterop;

namespace Shiny.Extensions.Stores.Web;

public class LocalStorageStore(IJSRuntime jsRuntime) : IKeyValueStore
{
    public string Alias => "settings";
    public bool IsReadOnly => false;

    public bool Remove(string key)
    {
        JS.InvokeVoid("localStorage.removeItem", key);
        return true;
    }

    public void Clear() => JS.InvokeVoid("localStorage.clear");
    public bool Contains(string key) => JS.Invoke<string>("localStorage.getItem", key) is not null;

    public object? Get(Type type, string key)
    {
        var json = JS.Invoke<string>("localStorage.getItem", key);
        return JsonSerializer.Deserialize(json, type);
    }

    public void Set(string key, object value)
    {
        var json = JsonSerializer.Serialize(value);
        JS.InvokeVoid("localStorage.setItem", key, json);
    }


    IJSInProcessRuntime? inprocJs;
    IJSInProcessRuntime JS => this.inprocJs ??= jsRuntime as IJSInProcessRuntime ?? throw new InvalidOperationException();
}