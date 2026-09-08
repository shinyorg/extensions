using System.Runtime.Versioning;

// Everything in this package sits on top of the GTK4 backend and the freedesktop.org command line
// tools, so the whole assembly is Linux-only. Declaring it here (rather than per member) means the
// platform-compatibility analyzer flags a call from a cross-platform code path in the consuming app.
[assembly: SupportedOSPlatform("linux")]
