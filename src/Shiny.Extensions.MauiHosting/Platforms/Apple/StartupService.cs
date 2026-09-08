#if MACCATALYST || MACOS
using System.Runtime.Versioning;
using CoreFoundation;
using ServiceManagement;

namespace Shiny.Impl;

public sealed partial class StartupService
{
    // SMAppService is macOS 13 / Mac Catalyst 16 and up. Earlier versions only had the deprecated
    // SMLoginItemSetEnabled helper-bundle route, which needs a separate signed helper bundle.
    // Nothing here touches MAUI Essentials, so IStartupService works on the AppKit head whether or
    // not the app wired up the macOS Essentials implementations.
#if MACOS
    [SupportedOSPlatformGuard("macos13.0")]
    public bool IsSupported => OperatingSystem.IsMacOSVersionAtLeast(13);
#else
    [SupportedOSPlatformGuard("maccatalyst16.0")]
    public bool IsSupported => OperatingSystem.IsMacCatalystVersionAtLeast(16);
#endif

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

        // Drives a UI transition into System Settings, so it has to run on the main thread. DispatchQueue
        // is used rather than MAUI's MainThread so this works on the AppKit head too.
        DispatchQueue.MainQueue.DispatchAsync(SMAppService.OpenSystemSettingsLoginItems);
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
