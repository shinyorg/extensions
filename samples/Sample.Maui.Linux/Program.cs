using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platforms.Linux.Gtk4.Platform;

namespace Sample.Maui;

// GtkMauiApplication owns the Gtk.Application loop and sets IPlatformApplication.Current, which is
// what ShinyHost hangs its service provider off.
public class Program : GtkMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public static void Main(string[] args) => new Program().Run(args);
}
