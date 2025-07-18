﻿//HintName: GeneratedRegistrations.g.cs
// <auto-generated />
using global::System.Linq;
using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tests;

public static class __GeneratedRegistrations
{
    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddGeneratedServices(
        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,
        params string[] categories
    )
    {
        if (categories?.Any(x => x.Equals("RecordCategory", global::System.StringComparison.OrdinalIgnoreCase)) == true)
        {
            services.AddTransient<global::TestNamespace.IMyService, global::TestNamespace.MyRecordService>();
        }

        return services;
    }
}
