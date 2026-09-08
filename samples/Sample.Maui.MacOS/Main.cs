using AppKit;

namespace Sample.Maui;

// The AppKit head owns its own entry point - there's no MAUI single-project generated Main on -macos.
public static class MainClass
{
    static void Main(string[] args)
    {
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new MauiMacOSApp();
        NSApplication.Main(args);
    }
}
