using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;

namespace Shiny.Impl;

/// <summary>
/// <see cref="IAppSupport"/> implementation backed by browser globals via <c>IJSInProcessRuntime</c>.
/// Requires <c>_content/Shiny.Extensions.BlazorHosting/shiny-appsupport.js</c> to be loaded via a
/// &lt;script&gt; tag in index.html before Shiny services are used.
/// </summary>
public sealed partial class AppSupport : IAppSupport, IDisposable
{
    // The browser exposes no event for locale/time-zone changes, so we poll like the MAUI
    // bare-TFM fallback does. The timers only run while something is subscribed.
    static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    readonly IJSInProcessRuntime js;
    readonly object syncLock = new();

    EventHandler<CultureInfo>? cultureHandler;
    EventHandler<TimeZoneInfo>? timeZoneHandler;
    Timer? culturePollTimer;
    Timer? timeZonePollTimer;

    string? userAgent;
    Version? userAgentVersion;
    bool userAgentLoaded;

    public AppSupport(IJSRuntime jsRuntime, Version appVersion)
    {
        // WebAssembly hosts implement IJSInProcessRuntime, which lets us read browser state
        // synchronously to back the interface's (non-async) properties.
        this.js = (IJSInProcessRuntime)jsRuntime;
        this.AppVersion = appVersion;
        this.CurrentCulture = CultureInfo.CurrentCulture;
        this.CurrentTimeZone = TimeZoneInfo.Local;
    }

    // Supplied by the head assembly at registration (e.g. ThisAssembly.AssemblyVersion) — no reflection.
    public Version AppVersion { get; }

    public string? UserAgent
    {
        get
        {
            this.EnsureUserAgent();
            return this.userAgent;
        }
    }

    public Version? UserAgentVersion
    {
        get
        {
            this.EnsureUserAgent();
            return this.userAgentVersion;
        }
    }

    public int ScreenWidth => this.js.Invoke<int>("shinyAppSupport.getScreenWidth");
    public int ScreenHeight => this.js.Invoke<int>("shinyAppSupport.getScreenHeight");
    public int BrowserWidth => this.js.Invoke<int>("shinyAppSupport.getBrowserWidth");
    public int BrowserHeight => this.js.Invoke<int>("shinyAppSupport.getBrowserHeight");

    public CultureInfo CurrentCulture { get; private set; }
    public TimeZoneInfo CurrentTimeZone { get; private set; }

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
            this.cultureHandler = null;
            this.timeZoneHandler = null;
            this.StopCulture();
            this.StopTimeZone();
        }
    }

    // The UA string is fixed for the page's lifetime, so read and parse it once.
    void EnsureUserAgent()
    {
        if (this.userAgentLoaded)
            return;

        this.userAgent = this.js.Invoke<string?>("shinyAppSupport.getUserAgent");
        this.userAgentVersion = ParseBrowserVersion(this.userAgent);
        this.userAgentLoaded = true;
    }

    void StartCulture()
        => this.culturePollTimer = new Timer(_ => this.RefreshCulture(), null, PollInterval, PollInterval);

    void StopCulture()
    {
        this.culturePollTimer?.Dispose();
        this.culturePollTimer = null;
    }

    void StartTimeZone()
        => this.timeZonePollTimer = new Timer(_ => this.RefreshTimeZone(), null, PollInterval, PollInterval);

    void StopTimeZone()
    {
        this.timeZonePollTimer?.Dispose();
        this.timeZonePollTimer = null;
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

    // Best-effort browser version from the UA string. Tokens are tried in specificity order
    // because the more-specific browsers also carry the less-specific tokens (e.g. Edge and
    // Opera both include "Chrome/", and Safari reports its own version under "Version/").
    static Version? ParseBrowserVersion(string? ua)
    {
        if (String.IsNullOrWhiteSpace(ua))
            return null;

        foreach (var regex in new[] { EdgeRegex(), OperaRegex(), FirefoxRegex(), ChromeRegex(), SafariRegex() })
        {
            var match = regex.Match(ua);
            if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
                return version;
        }
        return null;
    }

    [GeneratedRegex(@"Edg(?:[A-Z]+)?/(\d+(?:\.\d+){1,3})")]
    private static partial Regex EdgeRegex();

    [GeneratedRegex(@"OPR/(\d+(?:\.\d+){1,3})")]
    private static partial Regex OperaRegex();

    [GeneratedRegex(@"Firefox/(\d+(?:\.\d+){1,3})")]
    private static partial Regex FirefoxRegex();

    [GeneratedRegex(@"Chrome/(\d+(?:\.\d+){1,3})")]
    private static partial Regex ChromeRegex();

    [GeneratedRegex(@"Version/(\d+(?:\.\d+){1,3})")]
    private static partial Regex SafariRegex();
}
