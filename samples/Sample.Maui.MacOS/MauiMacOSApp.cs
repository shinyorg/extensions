using Foundation;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platforms.MacOS.Platform;

namespace Sample.Maui;

// MacOSMauiApplication is the NSApplicationDelegate that stands up the MAUI app and sets
// IPlatformApplication.Current, which is what ShinyHost hangs its service provider off.
[Register("MauiMacOSApp")]
public class MauiMacOSApp : MacOSMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
