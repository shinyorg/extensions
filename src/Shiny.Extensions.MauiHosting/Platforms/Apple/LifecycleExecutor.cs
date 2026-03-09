using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny;


public partial interface ILifecycleExecutor
{
    bool OnContinueUserActivity(NSUserActivity userActivity);
}

public partial class LifecycleExecutor(
    ILogger<LifecycleExecutor> logger,
    IServiceProvider services
) : ILifecycleExecutor
{
    public bool OnContinueUserActivity(NSUserActivity userActivity)
    {
        var handlers = services.GetServices<IContinueActivity>();
        foreach (var handler in handlers)
        {
            if (handler.Handle(userActivity, null))
                return true;
        }
        return false;
    }
}