using System.Globalization;

namespace Shiny;

public interface IAppSupport
{
    Version AppVersion { get; } // app version from head assembly - no reflection
    string? UserAgent { get; }
    Version? UserAgentVersion { get; }

    int ScreenWidth { get; }
    int ScreenHeight { get; }
    int BrowserWidth { get; }
    int BrowserHeight { get; }

    CultureInfo CurrentCulture { get; }
    event EventHandler<CultureInfo>? CultureChanged;

    TimeZoneInfo CurrentTimeZone { get; }
    event EventHandler<TimeZoneInfo>? TimeZoneChanged;
}
