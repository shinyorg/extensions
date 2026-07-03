using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.Stores;

namespace Shiny;


/// <summary>
/// Self-bootstrapping accessor for <see cref="IKeyValueStore"/>. On first access,
/// <see cref="Default"/> and <see cref="Secure"/> lazily create the platform-native
/// store (SharedPreferences/Keychain/NSUserDefaults/DPAPI). On desktop builds that
/// resolve the base <c>net10.0</c> asset (plain macOS, Linux, and unpackaged Windows)
/// they fall back to a persistent <see cref="FileKeyValueStore"/> so settings survive
/// restarts. On those desktop fallbacks the <see cref="Secure"/> store is a plain JSON
/// file and is <b>not</b> encrypted (except unpackaged Windows, which keeps DPAPI over
/// the file) — treat it as obfuscation, not protection, for genuinely sensitive data.
///
/// Calling <see cref="StoreExtensions.AddShinyStores"/> registers the same instances
/// into DI so that <c>IKeyValueStoreFactory</c> and keyed <c>IKeyValueStore</c>
/// injections share them.
///
/// For scenarios where the store cannot be statically constructed (e.g. Blazor's
/// LocalStorage store needs <c>IJSRuntime</c>) or to swap in a test double, call
/// <see cref="Register"/> before first access, or call
/// <see cref="StoreExtensions.UseShinyStores"/> after the service provider is built.
/// </summary>
public static class Stores
{
    static readonly object syncLock = new();
    static readonly Dictionary<object, IKeyValueStore> custom = new();
    static IKeyValueStore? defaultStore;
    static IKeyValueStore? secureStore;

    /// <summary>
    /// The shared <see cref="ISerializer"/> used by the static platform stores and
    /// registered into DI by <see cref="StoreExtensions.AddShinyStores"/>. Source-generated
    /// <c>[ShinyJsonInclude]</c> module initializers populate this before <c>Main</c>.
    /// Hand-written contexts may be added via <c>services.AddJsonContext(...)</c> or
    /// <see cref="Shiny.Json.AddContext"/>.
    /// </summary>
    public static ISerializer Serializer => Shiny.Json.Default;

    /// <summary>
    /// Overrides the directory used by the desktop file-backed store (<see cref="FileKeyValueStore"/>)
    /// on the base <c>net10.0</c> asset and on unpackaged Windows. When null (default), stores are
    /// placed under <c>{LocalApplicationData}/{EntryAssemblyName}</c>. Set this before first access to
    /// <see cref="Default"/>/<see cref="Secure"/>; it has no effect on Android/iOS/macCatalyst or
    /// packaged Windows, which use the platform-native store.
    /// </summary>
    public static string? FileStoreDirectory { get; set; }

    /// <summary>The default settings store (keyed <see cref="StoreKeys.Default"/>).</summary>
    public static IKeyValueStore Default
    {
        get
        {
            var s = defaultStore;
            if (s is not null)
                return s;

            lock (syncLock)
                return defaultStore ??= CreateDefault();
        }
    }

    /// <summary>The secure/encrypted store (keyed <see cref="StoreKeys.Secure"/>).</summary>
    public static IKeyValueStore Secure
    {
        get
        {
            var s = secureStore;
            if (s is not null)
                return s;

            lock (syncLock)
                return secureStore ??= CreateSecure();
        }
    }

    /// <summary>
    /// Resolves a store by arbitrary key. <see cref="StoreKeys.Default"/> and
    /// <see cref="StoreKeys.Secure"/> route to <see cref="Default"/>/<see cref="Secure"/>;
    /// other keys must have been registered via <see cref="Register"/> or
    /// <see cref="StoreExtensions.UseShinyStores"/>.
    /// </summary>
    public static IKeyValueStore Keyed(object key)
    {
        if (Equals(key, StoreKeys.Default))
            return Default;
        if (Equals(key, StoreKeys.Secure))
            return Secure;

        lock (syncLock)
        {
            if (custom.TryGetValue(key, out var s))
                return s;
        }

        throw new KeyNotFoundException(
            $"No store registered for key '{key}'. Call Shiny.Stores.Register(key, store) " +
            $"or register a keyed IKeyValueStore in DI and call serviceProvider.UseShinyStores()."
        );
    }

    /// <summary>
    /// Registers or overrides a store for the given key. Use for tests, Blazor
    /// (where <c>IJSRuntime</c> can't be constructed statically), or any custom
    /// store key referenced by <c>[ObjectStoreBinder("...")]</c>.
    /// </summary>
    public static void Register(object key, IKeyValueStore store)
    {
        if (store is null) throw new ArgumentNullException(nameof(store));
        lock (syncLock)
        {
            if (Equals(key, StoreKeys.Default))
                defaultStore = store;
            else if (Equals(key, StoreKeys.Secure))
                secureStore = store;
            else
                custom[key] = store;
        }
    }

    /// <summary>
    /// Snapshots all keyed <see cref="IKeyValueStore"/> registrations from
    /// the given provider into the static accessor. Equivalent to calling
    /// <see cref="StoreExtensions.UseShinyStores"/>.
    /// </summary>
    public static void Initialize(IServiceProvider provider)
    {
        if (provider is null) throw new ArgumentNullException(nameof(provider));

        SnapshotKey(provider, StoreKeys.Default);
        SnapshotKey(provider, StoreKeys.Secure);
    }

    internal static void SnapshotKey(IServiceProvider provider, object key)
    {
        var store = provider.GetKeyedService<IKeyValueStore>(key);
        if (store is not null)
            Register(key, store);
    }

    /// <summary>
    /// Clears all static registrations and forces <see cref="Default"/>/<see cref="Secure"/>
    /// to re-bootstrap on next access. Intended for tests and host-restart scenarios.
    /// </summary>
    public static void Reset()
    {
        lock (syncLock)
        {
            defaultStore = null;
            secureStore = null;
            custom.Clear();
        }
    }

    /// <summary>
    /// Creates an isolated scope for tests: resets all static registrations, then registers
    /// fresh in-memory stores for <see cref="StoreKeys.Default"/> and <see cref="StoreKeys.Secure"/>
    /// so [Bind]-backed types route through transient storage instead of the platform-native
    /// store. Disposing the returned scope clears the registrations again so the next test
    /// (or host run) starts clean.
    ///
    /// Because Shiny.Stores is static, tests using this helper must not run in parallel against
    /// each other — apply <c>[Collection("ShinyStores")]</c> (or any shared xUnit collection
    /// name) to every test class that depends on bound settings.
    /// </summary>
    /// <example>
    /// <code>
    /// public sealed class MyTests : IDisposable
    /// {
    ///     readonly IDisposable scope = Shiny.Stores.CreateTestScope();
    ///     public void Dispose() => scope.Dispose();
    /// }
    /// </code>
    /// </example>
    public static IDisposable CreateTestScope()
    {
        lock (syncLock)
        {
            defaultStore = new MemoryKeyValueStore();
            secureStore = new MemoryKeyValueStore();
            custom.Clear();
        }
        return new ResetOnDispose();
    }

    sealed class ResetOnDispose : IDisposable
    {
        int disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
                Reset();
        }
    }

    static IKeyValueStore CreateFileStore(string fileName)
        => new FileKeyValueStore(Path.Combine(ResolveStoreDirectory(), fileName), Serializer);

    static string ResolveStoreDirectory()
    {
        if (!String.IsNullOrWhiteSpace(FileStoreDirectory))
            return FileStoreDirectory!;

        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var app = Assembly.GetEntryAssembly()?.GetName().Name ?? "Shiny";
        return Path.Combine(root, app);
    }

#if ANDROID
    static IKeyValueStore CreateDefault() => new SettingsKeyValueStore(Serializer);
    static IKeyValueStore CreateSecure() => new SecureKeyValueStore(Serializer);
#elif IOS || MACCATALYST || __MACOS__
    // macOS (net10.0-macos) uses the same Foundation NSUserDefaults + Security.framework Keychain
    // APIs as iOS/Mac Catalyst, so plain macOS desktop apps get real secure storage (not the
    // plaintext desktop file fallback).
    static IKeyValueStore CreateDefault() => new SettingsKeyValueStore(Serializer);
    static IKeyValueStore CreateSecure() => new SecureKeyValueStore(Serializer);
#elif WINDOWS
    // Packaged (MSIX) apps get the native ApplicationData-backed store; unpackaged desktop apps
    // (WPF/WinForms/console targeting net10.0-windows) would throw on ApplicationData.Current, so
    // they fall back to the file store — with DPAPI still layered over Secure via SecureKeyValueStore.
    static IKeyValueStore CreateDefault()
        => WindowsPlatform.IsPackaged
            ? new SettingsKeyValueStore(Serializer)
            : CreateFileStore("settings.json");

    static IKeyValueStore CreateSecure()
        => WindowsPlatform.IsPackaged
            ? new SecureKeyValueStore(Serializer)
            : new SecureKeyValueStore(Serializer, CreateFileStore("secure.json"));
#else
    static IKeyValueStore CreateDefault() => CreateFileStore("settings.json");
    static IKeyValueStore CreateSecure() => CreateFileStore("secure.json");
#endif
}
