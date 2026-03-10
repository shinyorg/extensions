using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny;


public partial interface ILifecycleExecutor
{
    void OnFinishLaunching(NSDictionary launchOptions);
    bool OnContinueUserActivity(NSUserActivity userActivity);
}

public partial class LifecycleExecutor(
    ILogger<LifecycleExecutor> logger,
    IServiceProvider services
) : ILifecycleExecutor
{
    public void OnFinishLaunching(NSDictionary launchOptions) 
        => this.Execute<IOnFinishedLaunching>(x => x.Handle(launchOptions));
    
    public bool OnContinueUserActivity(NSUserActivity userActivity)
    {
        var handlers = services.GetServices<IContinueActivity>();
        foreach (var handler in handlers)
        {
            if (handler.Handle(userActivity))
                return true;
        }
        return false;
    }
}