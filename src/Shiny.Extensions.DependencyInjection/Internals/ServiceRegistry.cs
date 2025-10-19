using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.DependencyInjection.Internals;


public static class ServiceRegistry
{
    static readonly List<Action<IServiceCollection, IEnumerable<string>>> callbacks = new();
    
    public static void RegisterCallback(Action<IServiceCollection, IEnumerable<string>> callback)
    {
        callbacks.Add(callback);
    }


    public static void RunCallbacks(IServiceCollection services, params IEnumerable<string> categories)
    {
        callbacks.ForEach(x => x.Invoke(services, categories));
        callbacks.Clear();
    }
}