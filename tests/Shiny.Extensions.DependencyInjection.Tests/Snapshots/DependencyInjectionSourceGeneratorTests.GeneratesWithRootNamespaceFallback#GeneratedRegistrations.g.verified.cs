﻿//HintName: GeneratedRegistrations.g.cs
// <auto-generated />
using global::Microsoft.Extensions.DependencyInjection;

namespace MyApp.Extensions;

public static class __GeneratedRegistrations
{
    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddGeneratedServices(
        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services
    )
    {
        services.AddSingleton<global::TestNamespace.MyService>();

        return services;
    }
}
