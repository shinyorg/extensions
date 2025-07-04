﻿//HintName: GeneratedRegistrations.g.cs
// <auto-generated />
using global::Microsoft.Extensions.DependencyInjection;

namespace Tests;

public static class __GeneratedRegistrations
{
    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddGeneratedServices(
        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services
    )
    {
        services.AddSingleton(typeof(global::TestNamespace.IRepository<>), typeof(global::TestNamespace.Repository<>));
        services.AddTransient<global::TestNamespace.ILogger, global::TestNamespace.Logger>();
        services.AddKeyedScoped(typeof(global::TestNamespace.IRepository<>), typeof(global::TestNamespace.CachedRepository<>), "cached");

        return services;
    }
}
