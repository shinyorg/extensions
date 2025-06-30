using Microsoft.JSInterop;

namespace Shiny.Extensions.Stores.Web;


public class LocalStorageKeyValueStore(IJSRuntime jsRuntime) : BaseKeyValueStore(jsRuntime, "settings", "localStorage")
{
}