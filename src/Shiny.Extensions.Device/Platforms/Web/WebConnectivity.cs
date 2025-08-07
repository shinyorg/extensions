using Microsoft.JSInterop;

namespace Shiny.Extensions.Device;


public class WebConnectivity : BaseComponent, IConnectivity
{
    readonly DotNetObjectReference<WebConnectivity> dotNetRef;
    
    public WebConnectivity(IJSRuntime jsRuntime) : base(jsRuntime)
    {
        this.dotNetRef = DotNetObjectReference.Create(this);
        JS.InvokeVoid("ShinyServices.subscribe", this.dotNetRef);
    }
    
    public event EventHandler<bool>? StateChanged;
    public bool IsAvailable => JS.Invoke<bool>("ShinyServices.isOnline");


    [JSInvokable("ShinyServices.OnStatusChanged")]
    public void OnStatusChanged(bool isOnline)
    {
        this.StateChanged?.Invoke(this, isOnline);
    }
    
    public void Dispose() => JS.InvokeVoid("ShinyServices.unsubscribe");
}