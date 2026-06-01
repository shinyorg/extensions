using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;

namespace Shiny.Impl;

public sealed partial class AppSupport : IAppSupport, IDisposable
{
    // Used by the polling fallback only — native TFMs receive change notifications instead.
    static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    static readonly TimeSpan OrientationPollInterval = TimeSpan.FromSeconds(2);

    readonly object syncLock = new();
    EventHandler<DisplayOrientation>? orientationHandler;
    EventHandler<CultureInfo>? cultureHandler;
    EventHandler<TimeZoneInfo>? timeZoneHandler;

    public AppSupport()
    {
        this.CurrentOrientation = GetCurrentOrientation();
        this.CurrentCulture = CultureInfo.CurrentCulture;
        this.CurrentTimeZone = TimeZoneInfo.Local;
    }

    public Version AppVersion => AppInfo.Version;
    public string DeviceManufacturer => DeviceInfo.Manufacturer;
    public string DeviceModel => DeviceInfo.Model;
    public Version PlatformVersion => DeviceInfo.Version;

    public DisplayOrientation CurrentOrientation { get; private set; }
    public CultureInfo CurrentCulture { get; private set; }
    public TimeZoneInfo CurrentTimeZone { get; private set; }

    public event EventHandler<DisplayOrientation>? OrientationChanged
    {
        add
        {
            lock (this.syncLock)
            {
                var wasEmpty = this.orientationHandler == null;
                this.orientationHandler += value;
                if (wasEmpty)
                    this.StartOrientation();
            }
        }
        remove
        {
            lock (this.syncLock)
            {
                this.orientationHandler -= value;
                if (this.orientationHandler == null)
                    this.StopOrientation();
            }
        }
    }

    public event EventHandler<CultureInfo>? CultureChanged
    {
        add
        {
            lock (this.syncLock)
            {
                var wasEmpty = this.cultureHandler == null;
                this.cultureHandler += value;
                if (wasEmpty)
                    this.StartCulture();
            }
        }
        remove
        {
            lock (this.syncLock)
            {
                this.cultureHandler -= value;
                if (this.cultureHandler == null)
                    this.StopCulture();
            }
        }
    }

    public event EventHandler<TimeZoneInfo>? TimeZoneChanged
    {
        add
        {
            lock (this.syncLock)
            {
                var wasEmpty = this.timeZoneHandler == null;
                this.timeZoneHandler += value;
                if (wasEmpty)
                    this.StartTimeZone();
            }
        }
        remove
        {
            lock (this.syncLock)
            {
                this.timeZoneHandler -= value;
                if (this.timeZoneHandler == null)
                    this.StopTimeZone();
            }
        }
    }

    public void Dispose()
    {
        lock (this.syncLock)
        {
            this.orientationHandler = null;
            this.cultureHandler = null;
            this.timeZoneHandler = null;
            this.StopOrientation();
            this.StopCulture();
            this.StopTimeZone();
        }
    }

    public Task<bool> OpenBrowser(
        string uri,
        bool showTitle = true,
        BrowserLaunchMode launchMode = BrowserLaunchMode.SystemPreferred,
        BrowserLaunchFlags launchFlags = BrowserLaunchFlags.None,
        Color? preferredControlColor = null,
        Color? preferredToolbarColor = null
    ) => Browser.OpenAsync(
        new Uri(uri),
        new BrowserLaunchOptions
        {
            Flags = launchFlags,
            LaunchMode = launchMode,
            TitleMode = showTitle ? BrowserTitleMode.Show : BrowserTitleMode.Hide,
            PreferredControlColor = preferredControlColor,
            PreferredToolbarColor = preferredToolbarColor
        }
    );

    public Task<bool> OpenMap(
        double latitude,
        double longitude,
        NavigationMode navigationMode = NavigationMode.None
    ) => Map.TryOpenAsync(
        new Location(latitude, longitude),
        new MapLaunchOptions
        {
            Name = "",
            NavigationMode = navigationMode
        }
    );

    public void OpenAppSettings() => AppInfo.ShowSettingsUI();

    public Task<bool> SetOrientation(DisplayOrientation orientation)
        => this.ApplyOrientation(orientation);

    // Unknown is interpreted as "clear the lock" by every per-platform ApplyOrientation.
    public Task<bool> ResetOrientation()
        => this.ApplyOrientation(DisplayOrientation.Unknown);

    void RefreshOrientation(DisplayOrientation latest)
    {
        if (latest == this.CurrentOrientation)
            return;

        this.CurrentOrientation = latest;
        this.orientationHandler?.Invoke(this, latest);
    }

    void RefreshCulture()
    {
        CultureInfo.CurrentCulture.ClearCachedData();
        var latest = CultureInfo.CurrentCulture;
        if (latest.Equals(this.CurrentCulture))
            return;

        this.CurrentCulture = latest;
        this.cultureHandler?.Invoke(this, latest);
    }

    void RefreshTimeZone()
    {
        TimeZoneInfo.ClearCachedData();
        var latest = TimeZoneInfo.Local;
        if (latest.Equals(this.CurrentTimeZone))
            return;

        this.CurrentTimeZone = latest;
        this.timeZoneHandler?.Invoke(this, latest);
    }

    // DeviceDisplay throws NotImplementedInReferenceAssembly on the bare net10.0 TFM,
    // so we soak the exception and report Unknown for non-platform builds.
    static DisplayOrientation GetCurrentOrientation()
    {
        try
        {
            return DeviceDisplay.Current.MainDisplayInfo.Orientation;
        }
        catch
        {
            return DisplayOrientation.Unknown;
        }
    }

    // Orientation events are surfaced through MAUI's DeviceDisplay on every native TFM, so
    // the subscription itself is cross-platform. Only the polling fallback diverges.
#if ANDROID || IOS || MACCATALYST || WINDOWS
    void StartOrientation()
        => DeviceDisplay.Current.MainDisplayInfoChanged += this.OnDisplayInfoChanged;

    void StopOrientation()
        => DeviceDisplay.Current.MainDisplayInfoChanged -= this.OnDisplayInfoChanged;

    void OnDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
        => this.RefreshOrientation(e.DisplayInfo.Orientation);
#else
    System.Threading.Timer? orientationPollTimer;

    void StartOrientation()
        => this.orientationPollTimer = new System.Threading.Timer(
            _ => this.RefreshOrientation(GetCurrentOrientation()),
            null,
            OrientationPollInterval,
            OrientationPollInterval
        );

    void StopOrientation()
    {
        this.orientationPollTimer?.Dispose();
        this.orientationPollTimer = null;
    }
#endif

    // Culture and time-zone change events have no MAUI abstraction. The native partials in
    // Platforms/{Android,Apple,Windows}/AppSupport.cs each provide their own listener.
    // For the bare net10.0 TFM (no platform suffix), no platform file is compiled in, so the
    // polling fallback below kicks in instead.
#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
    System.Threading.Timer? culturePollTimer;
    System.Threading.Timer? timeZonePollTimer;

    void StartCulture()
        => this.culturePollTimer = new System.Threading.Timer(
            _ => this.RefreshCulture(),
            null,
            PollInterval,
            PollInterval
        );

    void StopCulture()
    {
        this.culturePollTimer?.Dispose();
        this.culturePollTimer = null;
    }

    void StartTimeZone()
        => this.timeZonePollTimer = new System.Threading.Timer(
            _ => this.RefreshTimeZone(),
            null,
            PollInterval,
            PollInterval
        );

    void StopTimeZone()
    {
        this.timeZonePollTimer?.Dispose();
        this.timeZonePollTimer = null;
    }

    // No supported way to lock orientation off-device — let callers know it didn't apply.
    Task<bool> ApplyOrientation(DisplayOrientation orientation) => Task.FromResult(false);
#endif
}
