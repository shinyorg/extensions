using System.Text.Json;
using Microsoft.JSInterop;

namespace Shiny.Extensions.Stores.Web;

public abstract class BaseKeyValueStore(IJSRuntime jsRuntime, string alias, string jsClass) : IKeyValueStore
{
    public string Alias => alias;
    public bool IsReadOnly => false;

    public bool Remove(string key)
    {
        JS.InvokeVoid($"{jsClass}.removeItem", key);
        return true;
    }

    public void Clear() => JS.InvokeVoid($"{jsClass}.clear");
    public bool Contains(string key) => JS.Invoke<string>($"{jsClass}.getItem", key) is not null;

    public object? Get(Type type, string key)
    {
        var json = JS.Invoke<string>($"{jsClass}.getItem", key);
        return JsonSerializer.Deserialize(json, type);
    }

    public void Set(string key, object value)
    {
        var json = JsonSerializer.Serialize(value);
        JS.InvokeVoid($"{jsClass}.setItem", key, json);
    }


    IJSInProcessRuntime? inprocJs;
    IJSInProcessRuntime JS => this.inprocJs ??= jsRuntime as IJSInProcessRuntime ?? throw new InvalidOperationException("This store is meant only for webassembly");
}