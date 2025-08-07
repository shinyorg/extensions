#if APP || WEB
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.Device;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShinyDeviceExtensions(this  IServiceCollection services)
    {
#if APP
        services.AddSingleton<IBattery, AppBattery>();
        services.AddSingleton<IConnectivity, AppConnectivity>();
        services.AddSingleton<ICultureInfo, AppCultureInfo>();
        services.AddSingleton<IDeviceInfo, AppDeviceInfo>();
        services.AddSingleton<ITimeZone, AppTimeZone>();
#elif WEB
        services.AddSingleton<IBattery, WebBattery>();
        services.AddSingleton<IConnectivity, WebConnectivity>();
        services.AddSingleton<ICultureInfo, WebCultureInfo>();
        services.AddSingleton<IDeviceInfo, WebDeviceInfo>();
        services.AddSingleton<ITimeZone, WebTimeZone>();
#endif
        return services;
    } 
}
#endif