using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;

namespace Shiny;

public interface IAppSupport
{
    Version AppVersion { get; }
    string DeviceManufacturer { get; }
    string DeviceModel { get; }
    Version? PlatformVersion { get; }
    string Platform { get; }
    DeviceIdiom DeviceIdiom { get; }
    
    
    DisplayOrientation CurrentOrientation { get; }
    event EventHandler<DisplayOrientation>? OrientationChanged;

    /// <summary>
    /// Requests that the OS lock the app to <paramref name="orientation"/>. Returns false
    /// if the platform cannot honour the request (e.g. orientation isn't permitted by the
    /// active view controller on iOS, or no current Activity on Android).
    /// </summary>
    Task<bool> SetOrientation(DisplayOrientation orientation);

    /// <summary>
    /// Clears any orientation lock set via <see cref="SetOrientation"/> and lets the OS
    /// decide based on device rotation and the platform's normal orientation rules.
    /// </summary>
    Task<bool> ResetOrientation();

    CultureInfo CurrentCulture { get; }
    event EventHandler<CultureInfo>? CultureChanged;

    TimeZoneInfo CurrentTimeZone { get; }
    event EventHandler<TimeZoneInfo>? TimeZoneChanged;

    Task<bool> OpenBrowser(
        string uri,
        bool showTitle = true,
        BrowserLaunchMode launchMode = BrowserLaunchMode.SystemPreferred,
        BrowserLaunchFlags launchFlags = BrowserLaunchFlags.None,
        Color? preferredControlColor = null,
        Color? preferredToolbarColor = null
    );

    Task<bool> OpenMap(
        double latitude,
        double longitude,
        NavigationMode navigationMode = NavigationMode.None
    );

    void OpenAppSettings();
}
