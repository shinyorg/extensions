using Microsoft.Extensions.Logging;

namespace Shiny;


public partial interface ILifecycleExecutor
{
}

public partial class LifecycleExecutor(    
    ILogger<LifecycleExecutor> logger,
    IServiceProvider services
) : ILifecycleExecutor
{
    
}