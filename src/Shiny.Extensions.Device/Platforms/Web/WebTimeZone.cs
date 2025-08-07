using Microsoft.JSInterop;

namespace Shiny.Extensions.Device;


public class WebTimeZone(IJSRuntime jsRuntime) : BaseComponent(jsRuntime), ITimeZone
{
    public TimeZoneInfo Current { get; }
    public event EventHandler<TimeZoneInfo>? Changed;
}