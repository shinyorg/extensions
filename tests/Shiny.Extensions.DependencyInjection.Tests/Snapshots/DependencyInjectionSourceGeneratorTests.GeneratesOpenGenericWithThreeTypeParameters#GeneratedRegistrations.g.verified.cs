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
        services.AddTransient(typeof(global::TestNamespace.ITripleProcessor<,,>), typeof(global::TestNamespace.TripleProcessor<,,>));

        return services;
    }
}
