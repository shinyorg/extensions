using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Shiny;

namespace Sample.Maui;

public partial class AppSupportPage : ContentPage
{
    readonly IAppSupport appSupport;
    readonly IStartupService startupService;

    public AppSupportPage()
    {
        this.InitializeComponent();

        // Host.Services is populated by Shiny once MAUI finishes initializing — safe to resolve here.
        this.appSupport = ShinyHost.Services.GetService(typeof(IAppSupport)) as IAppSupport
            ?? throw new InvalidOperationException("IAppSupport not registered — did you call AddAppSupport()?");

        this.startupService = ShinyHost.Services.GetService(typeof(IStartupService)) as IStartupService
            ?? throw new InvalidOperationException("IStartupService not registered — did you call AddStartupService()?");

        this.RenderDeviceInfo();
        this.RenderOrientation(this.appSupport.CurrentOrientation);
        this.RenderCulture(this.appSupport.CurrentCulture);
        this.RenderTimeZone(this.appSupport.CurrentTimeZone);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Subscribe lazily on appearing so the native listeners only run while this page is on screen.
        this.appSupport.OrientationChanged += this.OnOrientationChanged;
        this.appSupport.CultureChanged += this.OnCultureChanged;
        this.appSupport.TimeZoneChanged += this.OnTimeZoneChanged;

        // GetState reads from the OS every time — the user can flip the entry off outside the app.
        _ = this.RefreshStartupState();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.appSupport.OrientationChanged -= this.OnOrientationChanged;
        this.appSupport.CultureChanged -= this.OnCultureChanged;
        this.appSupport.TimeZoneChanged -= this.OnTimeZoneChanged;
    }

    void OnOrientationChanged(object? sender, DisplayOrientation orientation)
        => MainThread.BeginInvokeOnMainThread(() => this.RenderOrientation(orientation));

    void OnCultureChanged(object? sender, CultureInfo culture)
        => MainThread.BeginInvokeOnMainThread(() => this.RenderCulture(culture));

    void OnTimeZoneChanged(object? sender, TimeZoneInfo timeZone)
        => MainThread.BeginInvokeOnMainThread(() => this.RenderTimeZone(timeZone));

    async void OnLockPortrait(object? sender, EventArgs e)
        => await this.ApplyOrientation(DisplayOrientation.Portrait);

    async void OnLockLandscape(object? sender, EventArgs e)
        => await this.ApplyOrientation(DisplayOrientation.Landscape);

    async void OnResetOrientation(object? sender, EventArgs e)
    {
        var ok = await this.appSupport.ResetOrientation();
        this.OrientationResultLabel.Text = $"ResetOrientation → {(ok ? "applied" : "not supported on this platform")}";
    }

    async Task ApplyOrientation(DisplayOrientation orientation)
    {
        var ok = await this.appSupport.SetOrientation(orientation);
        this.OrientationResultLabel.Text = $"SetOrientation({orientation}) → {(ok ? "applied" : "not supported on this platform")}";
    }

    async void OnRegisterStartup(object? sender, EventArgs e) => await this.SetStartup(true);

    async void OnUnregisterStartup(object? sender, EventArgs e) => await this.SetStartup(false);

    async void OnOpenStartupSettings(object? sender, EventArgs e)
    {
        var opened = await this.startupService.OpenSettings();
        if (!opened)
            this.StartupStateLabel.Text = "Startup: no OS settings page to open on this platform";
    }

    async Task RefreshStartupState()
    {
        if (!this.startupService.IsSupported)
        {
            this.StartupStateLabel.Text = "Startup: not supported on this platform";
            return;
        }
        var state = await this.startupService.GetState();
        this.StartupStateLabel.Text = $"Startup: {state}";
    }

    async Task SetStartup(bool register)
    {
        if (!this.startupService.IsSupported)
        {
            this.StartupStateLabel.Text = "Startup: not supported on this platform";
            return;
        }

        try
        {
            var state = register
                ? await this.startupService.Register()
                : await this.startupService.Unregister();

            // RequiresApproval/DisabledByUser aren't failures — the user finishes the job in OS settings.
            this.StartupStateLabel.Text = $"Startup: {state}";
        }
        catch (Exception ex)
        {
            this.StartupStateLabel.Text = $"Startup failed: {ex.Message}";
        }
    }

    async void OnOpenBrowser(object? sender, EventArgs e)
        => await this.appSupport.OpenBrowser("https://shinylib.net/mauihost/");

    async void OnOpenMap(object? sender, EventArgs e)
        => await this.appSupport.OpenMap(latitude: 47.6062, longitude: -122.3321);

    void RenderDeviceInfo()
    {
        this.AppVersionLabel.Text = $"App version: {this.appSupport.AppVersion}";
        this.DeviceManufacturerLabel.Text = $"Manufacturer: {this.appSupport.DeviceManufacturer}";
        this.DeviceModelLabel.Text = $"Model: {this.appSupport.DeviceModel}";
        this.PlatformVersionLabel.Text = $"OS version: {this.appSupport.PlatformVersion}";
    }

    void RenderOrientation(DisplayOrientation orientation)
        => this.OrientationLabel.Text = $"Orientation: {orientation}";

    void RenderCulture(CultureInfo culture)
        => this.CultureLabel.Text = $"Culture: {culture.Name} ({culture.DisplayName})";

    void RenderTimeZone(TimeZoneInfo timeZone)
        => this.TimeZoneLabel.Text = $"Time zone: {timeZone.Id} (UTC{timeZone.BaseUtcOffset:hh\\:mm})";
}
