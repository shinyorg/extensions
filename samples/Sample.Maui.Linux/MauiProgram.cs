using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platforms.Linux.Gtk4.Hosting;
using Shiny;

namespace Sample.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiAppLinuxGtk4<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .AddInfrastructureModules()
            // AddLinuxAppSupport rather than AddAppSupport - the base implementation calls the static
            // MAUI Essentials APIs, which throw on Linux. This one uses the GTK4 Essentials services.
            .AddLinuxAppSupport()
            // Flatpak/Snap version lookups plus appstream:// deep links into the software centre.
            .AddLinuxAppStore("org.shiny.appsupport.sample")
            .AddStartupService(x =>
            {
                // Names the ~/.config/autostart/*.desktop entry this writes.
                x.DisplayName = "Shiny AppSupport Sample";
                x.Arguments.Add("--autostart");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
