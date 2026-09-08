using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platforms.Linux.Gtk4.Essentials.Hosting;
using Shiny.Impl;

namespace Shiny;

/// <summary>
/// Linux (GTK4) registrations for a .NET MAUI head built on the dotnet/maui-labs
/// <c>Microsoft.Maui.Platforms.Linux.Gtk4</c> backend.
/// <para>
/// These are deliberately named apart from the base package's AddAppSupport/AddAppStore. Both packages
/// put their extensions on <see cref="MauiAppBuilder"/> in the Shiny namespace, and the base
/// implementations talk to the static MAUI Essentials APIs, which throw on Linux.
/// </para>
/// </summary>
public static class LinuxHostingExtensions
{
    /// <summary>
    /// Registers <see cref="IAppSupport"/> for device info, browser/map launch helpers, and
    /// culture / time-zone change notifications on Linux.
    /// </summary>
    /// <remarks>
    /// Calls <c>AddLinuxGtk4Essentials</c> for you - <see cref="LinuxAppSupport"/> is built on the
    /// Essentials interfaces the GTK4 backend registers, not on the static Essentials APIs. It's
    /// TryAdd-based, so an app that already called it isn't affected.
    /// </remarks>
    public static MauiAppBuilder AddLinuxAppSupport(this MauiAppBuilder builder)
    {
        builder.AddLinuxGtk4Essentials();
        builder.Services.TryAddSingleton<IAppSupport, LinuxAppSupport>();
        return builder;
    }


    /// <summary>
    /// Registers <see cref="IAppStore"/> over Flatpak and Snap - version lookups against the remote the
    /// app was installed from, and <c>appstream://</c> deep links into the desktop's software centre.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure">
    /// Optional inline configuration of <see cref="AppStoreOptions"/>. Set
    /// <see cref="AppStoreOptions.LinuxAppId"/> when the app's AppStream/Flatpak ID differs from what the
    /// Flatpak or Snap environment reports - for a developer build outside a sandbox, it's the only way
    /// to identify the app.
    /// </param>
    public static MauiAppBuilder AddLinuxAppStore(
        this MauiAppBuilder builder,
        Action<AppStoreOptions>? configure = null
    )
    {
        if (configure != null)
            builder.Services.Configure(configure);

        builder.AddLinuxGtk4Essentials();
        builder.Services.TryAddSingleton<IAppStore, LinuxAppStore>();
        return builder;
    }


    /// <summary>
    /// Convenience overload for <see cref="AddLinuxAppStore(MauiAppBuilder, Action{AppStoreOptions})"/>
    /// that takes the AppStream component / Flatpak application ID directly.
    /// </summary>
    public static MauiAppBuilder AddLinuxAppStore(this MauiAppBuilder builder, string linuxAppId)
        => builder.AddLinuxAppStore(opts => opts.LinuxAppId = linuxAppId);
}
