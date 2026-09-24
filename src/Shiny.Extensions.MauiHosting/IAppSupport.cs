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
    /// Turns the device to <paramref name="orientation"/> and holds it there. Answers whether it ended
    /// up in that orientation - false where the platform cannot do it (iOS before 16, Mac Catalyst,
    /// macOS) or has nothing to do it with (no current Activity on Android).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>iOS</b>: holding the orientation needs one line in the app delegate - see
    /// <c>IosOrientationLock</c>. Without it the device turns once and is free to turn back.
    /// </para>
    /// <para>
    /// UIKit ignores an orientation change made while a navigation transition is animating. Call this
    /// once a page has appeared, not from a page that is disappearing - after the transition, not in it.
    /// </para>
    /// </remarks>
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
