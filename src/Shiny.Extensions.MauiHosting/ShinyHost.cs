using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Shiny;

public class ShinyHost : IMauiInitializeService
{
    public static IServiceProvider Services
    {
        get
        {
            if (field == null)
                throw new InvalidOperationException("Host isn't initialized yet");

            return field;
        }
        private set => field = value;
    }

    internal static List<IMauiModule> Modules { get; } = new();


    public void Initialize(IServiceProvider services)
    {
        var app = IPlatformApplication.Current!;
        Services = services;

        foreach (var module in Modules)
            module.Use(app);

        Modules.Clear();
    }
}
