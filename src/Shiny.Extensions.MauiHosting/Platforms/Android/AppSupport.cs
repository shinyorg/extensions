using Android.Content;
using Microsoft.Maui.Devices;

namespace Shiny.Impl;

public sealed partial class AppSupport
{
    LocaleReceiver? localeReceiver;
    TimeZoneReceiver? timeZoneReceiver;

    void StartCulture()
    {
        this.localeReceiver = new LocaleReceiver(this);
        RegisterReceiver(this.localeReceiver, Intent.ActionLocaleChanged);
    }

    void StopCulture()
    {
        if (this.localeReceiver == null)
            return;

        UnregisterReceiver(this.localeReceiver);
        this.localeReceiver.Dispose();
        this.localeReceiver = null;
    }

    void StartTimeZone()
    {
        this.timeZoneReceiver = new TimeZoneReceiver(this);
        RegisterReceiver(this.timeZoneReceiver, Intent.ActionTimezoneChanged);
    }

    void StopTimeZone()
    {
        if (this.timeZoneReceiver == null)
            return;

        UnregisterReceiver(this.timeZoneReceiver);
        this.timeZoneReceiver.Dispose();
        this.timeZoneReceiver = null;
    }

    // Sensor variants let the device flip between left/right (or normal/flipped) while staying
    // within the chosen orientation, rather than locking to a single physical rotation.
    Task<bool> ApplyOrientation(DisplayOrientation orientation)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null)
            return Task.FromResult(false);

        activity.RequestedOrientation = orientation switch
        {
            DisplayOrientation.Portrait => Android.Content.PM.ScreenOrientation.SensorPortrait,
            DisplayOrientation.Landscape => Android.Content.PM.ScreenOrientation.SensorLandscape,
            _ => Android.Content.PM.ScreenOrientation.Unspecified
        };
        return Task.FromResult(true);
    }

    // Android 14+ requires runtime receivers to specify an export flag. Locale/timezone broadcasts
    // are protected system broadcasts, so NotExported is the correct value.
    static void RegisterReceiver(BroadcastReceiver receiver, string action)
    {
        var ctx = Android.App.Application.Context;
        var filter = new IntentFilter(action);

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            ctx.RegisterReceiver(receiver, filter, ReceiverFlags.NotExported);
        else
            ctx.RegisterReceiver(receiver, filter);
    }

    static void UnregisterReceiver(BroadcastReceiver receiver)
    {
        try
        {
            Android.App.Application.Context.UnregisterReceiver(receiver);
        }
        catch
        {
            // Swallow — Android throws IllegalArgumentException if the receiver was never registered,
            // which can happen during dispose paths after a partial failure.
        }
    }

    sealed class LocaleReceiver(AppSupport service) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action == Intent.ActionLocaleChanged)
                service.RefreshCulture();
        }
    }

    sealed class TimeZoneReceiver(AppSupport service) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action == Intent.ActionTimezoneChanged)
                service.RefreshTimeZone();
        }
    }
}
