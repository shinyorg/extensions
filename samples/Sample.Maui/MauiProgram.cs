using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Shiny;

namespace Sample.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            // Opt into each Shiny capability explicitly. Platform lifecycle is wired by UseShiny.
            .AddInfrastructureModules()
            .AddAppSupport()
            .AddStartupService(x =>
            {
                // Only used by the Linux desktop entry; Windows and macOS name the entry themselves.
                x.DisplayName = "Shiny AppSupport Sample";
                // A marker argument lets the app detect a login launch and start minimized.
                x.Arguments.Add("--autostart");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
