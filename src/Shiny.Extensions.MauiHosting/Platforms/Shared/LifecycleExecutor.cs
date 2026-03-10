using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny;

public partial interface ILifecycleExecutor
{
    void OnAppForeground();
    void OnAppBackground();
}

public partial class LifecycleExecutor : ILifecycleExecutor
{
    public void OnAppForeground() => this.Execute<IAppForeground>(x => x.OnForeground());
    public void OnAppBackground() => this.Execute<IAppBackground>(x => x.OnBackground());


    protected virtual void Execute<T>(Action<T> action)
    {
        var handlers = services.GetServices<T>();
        foreach (var handler in handlers)
        {
            try
            {
                action.Invoke(handler);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error executing lifecycle event for {HandlerType}", handler.GetType().FullName);
            }
        }
    }
}