using System.Globalization;
using Microsoft.JSInterop;

namespace Shiny.Extensions.Device;


public class WebCultureInfo(IJSRuntime jsRuntime) : BaseComponent(jsRuntime), ICultureInfo
{
    public CultureInfo Current { get; }
    public event EventHandler<CultureInfo>? Changed;
}