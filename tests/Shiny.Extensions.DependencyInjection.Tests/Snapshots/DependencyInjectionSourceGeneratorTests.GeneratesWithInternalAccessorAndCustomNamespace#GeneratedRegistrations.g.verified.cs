﻿//HintName: GeneratedRegistrations.g.cs
// <auto-generated />
using global::System.Linq;
using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.DependencyInjection.Extensions;

namespace Internal.Extensions;

internal static class __GeneratedRegistrations
{
    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddInternalServices(
        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,
        params string[] categories
    )
    {
        services.AddScoped<global::TestNamespace.MyInternalService>();

        return services;
    }
}
