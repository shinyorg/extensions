using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Infrastructure;

namespace Shiny;


public static class SerializerExtensions
{
    /// <summary>
    /// Adds a <see cref="JsonSerializerContext"/> to the shared
    /// <see cref="Stores.Serializer"/> so its types can be serialized in an
    /// AOT-compatible way, and ensures the serializer is registered in DI.
    /// </summary>
    public static IServiceCollection AddJsonContext(this IServiceCollection services, JsonSerializerContext context)
    {
        Stores.Serializer.AddContext(context);
        services.TryAddSingleton<ISerializer>(Stores.Serializer);
        return services;
    }
}
