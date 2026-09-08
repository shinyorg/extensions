using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;

namespace Shiny.Impl;

/// <summary>
/// Linux (GTK4) implementation of <see cref="IAppSupport"/>.
/// <para>
/// The base package's AppSupport talks to the static MAUI Essentials APIs (AppInfo.Version,
/// DeviceInfo.Model, Browser.OpenAsync, ...). On Linux those statics resolve the platform-neutral
/// Microsoft.Maui.Essentials asset and throw NotImplementedInReferenceAssembly, and
/// AddLinuxGtk4Essentials only redirects five of them. So this implementation resolves the
/// Essentials interfaces out of the container instead, which is where the GTK4 backend registers them.
/// </para>
/// </summary>
public sealed class LinuxAppSupport : IAppSupport, IDisposable
{
    // Neither the locale nor the display raises a change signal on this backend - see the comments
    // on StartCulture and StartOrientation for why each falls back to a poll.
    static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    readonly IAppInfo appInfo;
    readonly IDeviceInfo deviceInfo;
    readonly IDeviceDisplay deviceDisplay;
    readonly IBrowser browser;
    readonly IMap map;

    readonly object syncLock = new();
    EventHandler<DisplayOrientation>? orientationHandler;
    EventHandler<CultureInfo>? cultureHandler;
    EventHandler<TimeZoneInfo>? timeZoneHandler;

    public LinuxAppSupport(
        IAppInfo appInfo,
        IDeviceInfo deviceInfo,
        IDeviceDisplay deviceDisplay,
        IBrowser browser,
        IMap map
    )
    {
        this.appInfo = appInfo;
        this.deviceInfo = deviceInfo;
        this.deviceDisplay = deviceDisplay;
        this.browser = browser;
        this.map = map;

        this.CurrentOrientation = this.GetCurrentOrientation();
        this.CurrentCulture = CultureInfo.CurrentCulture;
        this.CurrentTimeZone = TimeZoneInfo.Local;
    }

    public Version AppVersion => this.appInfo.Version;
    public string DeviceManufacturer => this.deviceInfo.Manufacturer;
    public string DeviceModel => this.deviceInfo.Model;
    public Version? PlatformVersion => this.deviceInfo.Version;
    public string Platform => this.deviceInfo.Platform.ToString();
    public DeviceIdiom DeviceIdiom => this.deviceInfo.Idiom;

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
    ) => this.browser.OpenAsync(
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
    ) => this.map.TryOpenAsync(
        latitude,
        longitude,
        new MapLaunchOptions
        {
            Name = "",
            NavigationMode = navigationMode
        }
    );

    public void OpenAppSettings() => this.appInfo.ShowSettingsUI();

    // Desktop windows don't rotate and no desktop environment lets an app dictate screen orientation,
    // so both of these report that the request didn't apply rather than pretending it did.
    public Task<bool> SetOrientation(DisplayOrientation orientation) => Task.FromResult(false);
    public Task<bool> ResetOrientation() => Task.FromResult(false);


    #region Orientation

    // The GTK4 backend exposes MainDisplayInfoChanged but never raises it today, so the subscription is
    // there to pick the event up for free if that changes, with a slow poll doing the actual work.
    Timer? orientationPollTimer;

    void StartOrientation()
    {
        this.deviceDisplay.MainDisplayInfoChanged += this.OnDisplayInfoChanged;
        this.orientationPollTimer = new Timer(
            _ => this.RefreshOrientation(this.GetCurrentOrientation()),
            null,
            PollInterval,
            PollInterval
        );
    }

    void StopOrientation()
    {
        this.deviceDisplay.MainDisplayInfoChanged -= this.OnDisplayInfoChanged;
        this.orientationPollTimer?.Dispose();
        this.orientationPollTimer = null;
    }

    void OnDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
        => this.RefreshOrientation(e.DisplayInfo.Orientation);

    DisplayOrientation GetCurrentOrientation()
    {
        try
        {
            return this.deviceDisplay.MainDisplayInfo.Orientation;
        }
        catch
        {
            // No GDK display (headless session, GTK not initialized yet) - the backend normally soaks
            // this itself, but a custom IDeviceDisplay registration might not.
            return DisplayOrientation.Unknown;
        }
    }

    void RefreshOrientation(DisplayOrientation latest)
    {
        if (latest == this.CurrentOrientation)
            return;

        this.CurrentOrientation = latest;
        this.orientationHandler?.Invoke(this, latest);
    }

    #endregion

    #region Culture

    // .NET resolves CurrentCulture from LANG/LC_ALL when the process starts, and a running process never
    // sees those change - a Linux locale switch takes effect on the next login. The poll is here so a
    // culture the app itself sets (CultureInfo.DefaultThreadCurrentCulture) still raises the event.
    Timer? culturePollTimer;

    void StartCulture()
        => this.culturePollTimer = new Timer(_ => this.RefreshCulture(), null, PollInterval, PollInterval);

    void StopCulture()
    {
        this.culturePollTimer?.Dispose();
        this.culturePollTimer = null;
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

    #endregion

    #region TimeZone

    const string LocalTimeDirectory = "/etc";
    const string LocalTimeFile = "localtime";

    // Unlike the locale, the time zone genuinely does change under a running process: systemd-timedated
    // replaces the /etc/localtime symlink, and TimeZoneInfo picks the new zone up after ClearCachedData.
    // The watcher is on the directory because the symlink is replaced rather than edited in place.
    FileSystemWatcher? timeZoneWatcher;
    Timer? timeZonePollTimer;

    void StartTimeZone()
    {
        try
        {
            this.timeZoneWatcher = new FileSystemWatcher(LocalTimeDirectory, LocalTimeFile)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };
            this.timeZoneWatcher.Changed += this.OnLocalTimeChanged;
            this.timeZoneWatcher.Created += this.OnLocalTimeChanged;
            this.timeZoneWatcher.Deleted += this.OnLocalTimeChanged;
            this.timeZoneWatcher.Renamed += this.OnLocalTimeChanged;
            this.timeZoneWatcher.EnableRaisingEvents = true;
        }
        catch (Exception)
        {
            // No inotify watches left, or /etc isn't readable (a tightly sandboxed Flatpak) - fall back
            // to polling rather than dropping the event on the floor.
            this.timeZoneWatcher?.Dispose();
            this.timeZoneWatcher = null;
            this.timeZonePollTimer = new Timer(_ => this.RefreshTimeZone(), null, PollInterval, PollInterval);
        }
    }

    void StopTimeZone()
    {
        this.timeZoneWatcher?.Dispose();
        this.timeZoneWatcher = null;
        this.timeZonePollTimer?.Dispose();
        this.timeZonePollTimer = null;
    }

    void OnLocalTimeChanged(object? sender, FileSystemEventArgs e) => this.RefreshTimeZone();

    void RefreshTimeZone()
    {
        TimeZoneInfo.ClearCachedData();
        var latest = TimeZoneInfo.Local;
        if (latest.Equals(this.CurrentTimeZone))
            return;

        this.CurrentTimeZone = latest;
        this.timeZoneHandler?.Invoke(this, latest);
    }

    #endregion
}
