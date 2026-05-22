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
            // The four Shiny calls — opt into each capability explicitly.
            .AddInfrastructureModules()
            .AddPlatformLifecycle()
            .AddAppSupport();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
