using Microsoft.Extensions.Hosting;

namespace Shiny.Extensions.Stores.Infrastructure;


internal class StoresInitializer(IServiceProvider services) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Shiny.Stores.Initialize(services);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
