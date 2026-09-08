#if MACCATALYST
using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;
using ServiceManagement;

namespace Shiny.Impl;

public sealed partial class StartupService
{
    // SMAppService is macOS 13 / Mac Catalyst 16 and up. Earlier versions only had the deprecated
    // SMLoginItemSetEnabled helper-bundle route, which a Catalyst app can't use anyway.
    [SupportedOSPlatformGuard("maccatalyst16.0")]
    public bool IsSupported => OperatingSystem.IsMacCatalystVersionAtLeast(16);

    Task<StartupServiceState> GetStateCore(CancellationToken cancellationToken)
        => Task.FromResult(
            this.IsSupported
                ? ToState(SMAppService.MainApp.Status)
                : StartupServiceState.NotSupported
        );


    Task<StartupServiceState> RegisterCore(CancellationToken cancellationToken)
    {
        if (!this.IsSupported)
            return Task.FromResult(StartupServiceState.NotSupported);

        var service = SMAppService.MainApp;

        // Registering an app that is already a login item comes back as a failure (kSMErrorAlreadyRegistered),
        // so the resulting status - not the bool - is what decides whether this actually went wrong.
        if (!service.Register(out var error) && service.Status != SMAppServiceStatus.Enabled)
        {
            throw new InvalidOperationException(
                "Could not register the app as a macOS login item: " +
                (error?.LocalizedDescription ?? "unknown error")
            );
        }
        return Task.FromResult(ToState(service.Status));
    }


    Task<StartupServiceState> UnregisterCore(CancellationToken cancellationToken)
    {
        if (!this.IsSupported)
            return Task.FromResult(StartupServiceState.NotSupported);

        var service = SMAppService.MainApp;
        if (!service.Unregister(out var error) && service.Status == SMAppServiceStatus.Enabled)
        {
            throw new InvalidOperationException(
                "Could not remove the app from the macOS login items: " +
                (error?.LocalizedDescription ?? "unknown error")
            );
        }
        return Task.FromResult(ToState(service.Status));
    }


    Task<bool> OpenSettingsCore()
    {
        if (!this.IsSupported)
            return Task.FromResult(false);

        // Drives a UI transition into System Settings, so it has to run on the main thread.
        MainThread.BeginInvokeOnMainThread(SMAppService.OpenSystemSettingsLoginItems);
        return Task.FromResult(true);
    }


    // NotFound means macOS can no longer resolve the registered item (usually the bundle moved or the
    // registration was cleared out), which is the same thing as "not registered" to a caller.
    static StartupServiceState ToState(SMAppServiceStatus status) => status switch
    {
        SMAppServiceStatus.Enabled => StartupServiceState.Enabled,
        SMAppServiceStatus.RequiresApproval => StartupServiceState.RequiresApproval,
        _ => StartupServiceState.NotRegistered
    };
}
#endif
