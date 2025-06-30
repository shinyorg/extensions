using Microsoft.JSInterop;

namespace Shiny.Extensions.Stores.Web;

public class SessionStorageKeyValueStore(IJSRuntime jsRuntime) : BaseKeyValueStore(jsRuntime, "session", "sessionStorage")
{
}