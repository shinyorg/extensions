using Microsoft.Extensions.DependencyInjection;

namespace Shiny;

public partial interface ILifecycleExecutor
{
    void OnAppForeground();
    void OnAppBackground();
}

public partial class LifecycleExecutor : ILifecycleExecutor
{
    public void OnAppForeground()
    {
        var handlers = services.GetServices<IAppForeground>();
        foreach (var handler in handlers)
            handler.OnForeground();
    }
    
    public void OnAppBackground()
    {
        var handlers = services.GetServices<IAppBackground>();
        foreach (var handler in handlers)
            handler.OnBackground();
    }


    protected virtual void Execute<T>(Action<T> action)
    {
        
    }
}