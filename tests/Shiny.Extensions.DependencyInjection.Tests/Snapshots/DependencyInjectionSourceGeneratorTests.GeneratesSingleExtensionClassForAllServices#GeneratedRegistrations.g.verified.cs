﻿//HintName: GeneratedRegistrations.g.cs
// <auto-generated />
using global::Microsoft.Extensions.DependencyInjection;

namespace AllServices.Extensions;

public static class __GeneratedRegistrations
{
    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddAllAssemblyServices(
        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services
    )
    {
        services.AddSingleton<global::FirstNamespace.FirstService>();
        services.AddTransient<global::SecondNamespace.SecondService>();
        services.AddScoped<global::ThirdNamespace.IThirdService, global::ThirdNamespace.ThirdService>();

        return services;
    }
}
