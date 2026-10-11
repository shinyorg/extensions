using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Shiny.Extensions.Stores.Web;


/// <summary>
/// Synchronous wrapper over the browser's <c>window.localStorage</c>. Calls the Web Storage API
/// directly through <see cref="IJSInProcessRuntime"/>, so no companion script needs to be loaded.
/// </summary>
/// <remarks>
/// JSInterop marshals arguments/results through System.Text.Json; everything crossing the boundary
/// here is a <see cref="string"/> or <see cref="int"/>, which is trim-safe.
/// </remarks>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only string/int values cross the JS boundary")]
sealed class LocalStorage(IJSRuntime jsRuntime)
{
    readonly IJSInProcessRuntime js = jsRuntime as IJSInProcessRuntime
        ?? throw new InvalidOperationException("localStorage stores require Blazor WebAssembly (IJSInProcessRuntime)");


    public string? GetItem(string key)
        => this.js.Invoke<string?>("localStorage.getItem", key);


    public void SetItem(string key, string value)
        => this.js.InvokeVoid("localStorage.setItem", key, value);


    public bool ContainsKey(string key)
        => this.GetItem(key) != null;


    public bool RemoveItem(string key)
    {
        var existed = this.ContainsKey(key);
        this.js.InvokeVoid("localStorage.removeItem", key);
        return existed;
    }


    public string[] GetKeys(string prefix)
    {
        var length = this.js.GetValue<int>("localStorage.length");
        var keys = new List<string>();

        for (var i = 0; i < length; i++)
        {
            var key = this.js.Invoke<string?>("localStorage.key", i);
            if (key != null && key.StartsWith(prefix, StringComparison.Ordinal))
                keys.Add(key);
        }
        return keys.ToArray();
    }


    public int RemoveKeys(string prefix)
    {
        // collect first - removing while walking localStorage.key(i) shifts the indexes
        var keys = this.GetKeys(prefix);
        foreach (var key in keys)
            this.js.InvokeVoid("localStorage.removeItem", key);

        return keys.Length;
    }
}
