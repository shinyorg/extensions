using Microsoft.JSInterop;

namespace Shiny.Extensions.Device;


public class WebBattery(IJSRuntime jsRuntime) : BaseComponent(jsRuntime), IBattery
{
    
}