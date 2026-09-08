using System.Runtime.Versioning;

// The GTK4 head only ever runs on Linux. Declaring it once here keeps the platform-compatibility
// analyzer quiet about every UseMauiAppLinuxGtk4/AddLinux* call instead of suppressing them one by one.
[assembly: SupportedOSPlatform("linux")]
