using Microsoft.JSInterop;

namespace Shiny.Extensions.Device;


public class WebDeviceInfo(IJSRuntime jsRuntime) : BaseComponent(jsRuntime), IDeviceInfo
{
    public string OperatingSystem { get; }
    public string? OperatingSystemVersion { get; }
    public string Manufacturer { get; }
    public string? Model { get; }
}