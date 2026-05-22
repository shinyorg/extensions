using Microsoft.Maui.Devices;
using Microsoft.Win32;

namespace Shiny.Impl;

public sealed partial class AppSupport
{
    void StartCulture()
        => SystemEvents.UserPreferenceChanged += this.OnUserPreferenceChanged;

    void StopCulture()
        => SystemEvents.UserPreferenceChanged -= this.OnUserPreferenceChanged;

    void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.Locale)
            this.RefreshCulture();
    }

    void StartTimeZone()
        => SystemEvents.TimeChanged += this.OnTimeChanged;

    void StopTimeZone()
        => SystemEvents.TimeChanged -= this.OnTimeChanged;

    void OnTimeChanged(object? sender, EventArgs e)
        => this.RefreshTimeZone();

    // AutoRotationPreferences locks the app to the chosen physical orientation(s); setting it to
    // DisplayOrientations.None restores the system default (rotates with the device).
    Task<bool> ApplyOrientation(DisplayOrientation orientation)
    {
        Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences = orientation switch
        {
            DisplayOrientation.Portrait => Windows.Graphics.Display.DisplayOrientations.Portrait,
            DisplayOrientation.Landscape => Windows.Graphics.Display.DisplayOrientations.Landscape,
            _ => Windows.Graphics.Display.DisplayOrientations.None
        };
        return Task.FromResult(true);
    }
}
