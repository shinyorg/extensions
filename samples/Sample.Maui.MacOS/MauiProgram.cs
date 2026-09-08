using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platforms.MacOS.Hosting;
using Shiny;

namespace Sample.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiAppMacOS<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .AddInfrastructureModules()
            // Wires the AppKit Essentials implementations in for you - the static MAUI Essentials
            // APIs have no macOS implementation of their own.
            .AddAppSupport()
            .AddStartupService(x =>
            {
                x.DisplayName = "Shiny AppSupport Sample";
                x.Arguments.Add("--autostart");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
