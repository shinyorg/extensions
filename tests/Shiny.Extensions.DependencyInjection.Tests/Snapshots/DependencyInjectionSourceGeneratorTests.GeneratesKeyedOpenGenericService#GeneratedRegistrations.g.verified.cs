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
        services.AddKeyedSingleton(typeof(global::TestNamespace.IGenericHandler<>), typeof(global::TestNamespace.SpecialGenericHandler<>), "special");

        return services;
    }
}
