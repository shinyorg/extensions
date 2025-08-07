using Microsoft.JSInterop;

namespace Shiny.Extensions.Device;


public class BaseComponent(IJSRuntime jsRuntime)
{
    public JSInProcessRuntime JS => (JSInProcessRuntime)jsRuntime;
}