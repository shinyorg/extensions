using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Shiny.Impl;

namespace Shiny;


/// <summary>
/// Static accessor for the shared <see cref="ISerializer"/>. Contexts, custom resolvers,
/// and option configurators registered here flow through both the static <see cref="Default"/>
/// accessor and the DI-resolved <see cref="ISerializer"/> registered by
/// <c>services.AddJsonSerialization()</c>.
///
/// Source-generated module initializers (emitted for assemblies that use
/// <c>[ShinyJsonContext]</c> or <c>[ShinyJsonInclude]</c>) call <see cref="AddContext"/> /
/// <see cref="AddResolver"/> before <c>Main</c>, so the shared chain is populated before
/// any DI runs and before <see cref="Shiny.Stores"/> bootstraps its platform-native stores
/// on mobile.
/// </summary>
public static class Json
{
    static readonly object syncLock = new();
    static readonly List<IJsonTypeInfoResolver> resolvers = new();
    static readonly List<Action<JsonSerializerOptions>> configurators = new();
    static ISerializer? cached;


    /// <summary>The shared serializer. Built on first access from registered resolvers and configurators.</summary>
    public static ISerializer Default
    {
        get
        {
            var c = cached;
            if (c is not null)
                return c;

            lock (syncLock)
                return cached ??= Build();
        }
    }


    /// <summary>
    /// Replaces the shared serializer with a custom <see cref="ISerializer"/>. Rare —
    /// most consumers should extend the built-in <see cref="Impl.DefaultJsonSerializer"/>
    /// via <see cref="AddContext"/>/<see cref="AddResolver"/>/<see cref="Configure"/> instead.
    ///
    /// Once set, registered resolvers and configurators no longer apply (they would have
    /// nowhere to attach). Call <see cref="Reset"/> to return to the default lazy-built
    /// serializer with the registered resolvers/configurators re-applied.
    ///
    /// Must be called before first access to <see cref="Default"/> (or <see cref="Shiny.Stores.Default"/>/
    /// <see cref="Shiny.Stores.Secure"/>) — consumers that captured the previous instance
    /// in their constructor will not see the replacement.
    /// </summary>
    public static void SetDefault(ISerializer serializer)
    {
        if (serializer is null) throw new ArgumentNullException(nameof(serializer));
        lock (syncLock)
            cached = serializer;
    }


    /// <summary>
    /// Adds a <see cref="JsonSerializerContext"/> to the shared chain. Safe to call
    /// from a <c>[ModuleInitializer]</c>, from DI extensions, or directly.
    /// </summary>
    public static void AddContext(JsonSerializerContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        AddResolver(context);
    }


    /// <summary>
    /// Adds a custom <see cref="IJsonTypeInfoResolver"/> to the shared chain. Used by the
    /// generator-emitted collection wrappers for <c>[ShinyJsonInclude]</c> types and by
    /// any consumer that wants to plug in a hand-rolled resolver alongside source-generated contexts.
    /// </summary>
    public static void AddResolver(IJsonTypeInfoResolver resolver)
    {
        if (resolver is null) throw new ArgumentNullException(nameof(resolver));

        lock (syncLock)
        {
            // Ignore duplicate registrations of the same resolver/context. The same singleton
            // context or generated resolver can be installed from more than one path — a
            // [ModuleInitializer], a DI extension, or a manual call. The first install happens
            // before the serializer is built; a later duplicate install runs after Default has
            // been built and used, at which point its JsonSerializerOptions are frozen and
            // TypeInfoResolverChain.Add would throw. The type is already in the chain from the
            // first install, so the duplicate is a no-op.
            foreach (var existing in resolvers)
            {
                if (ReferenceEquals(existing, resolver) || existing.GetType() == resolver.GetType())
                    return;
            }

            resolvers.Add(resolver);
            if (cached is DefaultJsonSerializer dj)
                dj.Options.TypeInfoResolverChain.Add(resolver);
        }
    }


    /// <summary>
    /// Registers a callback that mutates <see cref="JsonSerializerOptions"/> before the
    /// serializer is used. Throws on mutating already-frozen options (e.g. after first use).
    /// </summary>
    public static void Configure(Action<JsonSerializerOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        lock (syncLock)
        {
            configurators.Add(configure);
            if (cached is DefaultJsonSerializer dj)
                configure(dj.Options);
        }
    }


    /// <summary>
    /// Clears the built serializer. Registered resolvers and configurators are kept and
    /// re-applied on next access. Intended for tests so a fresh, unfrozen
    /// <see cref="JsonSerializerOptions"/> is produced.
    /// </summary>
    public static void Reset()
    {
        lock (syncLock)
            cached = null;
    }


    /// <summary>
    /// Adds extra resolvers/configurators for the scope and returns an <see cref="IDisposable"/>
    /// that removes them and <see cref="Reset"/>s the cached serializer on dispose.
    /// Tests using this helper must not run in parallel against each other — apply
    /// <c>[Collection("ShinyJson")]</c> to every test class that depends on the shared chain.
    /// </summary>
    public static IDisposable CreateTestScope(
        IEnumerable<IJsonTypeInfoResolver>? extraResolvers = null,
        Action<JsonSerializerOptions>? extraConfigure = null
    )
    {
        int resolverSnapshot, configSnapshot;
        lock (syncLock)
        {
            resolverSnapshot = resolvers.Count;
            configSnapshot = configurators.Count;

            if (extraResolvers is not null)
                foreach (var r in extraResolvers)
                    resolvers.Add(r);

            if (extraConfigure is not null)
                configurators.Add(extraConfigure);

            cached = null;
        }

        return new TrimOnDispose(resolverSnapshot, configSnapshot);
    }


    static DefaultJsonSerializer Build()
    {
        var s = new DefaultJsonSerializer();
        foreach (var cfg in configurators)
            cfg(s.Options);
        foreach (var r in resolvers)
            s.Options.TypeInfoResolverChain.Add(r);
        return s;
    }


    sealed class TrimOnDispose : IDisposable
    {
        readonly int resolverSnapshot;
        readonly int configSnapshot;
        int disposed;

        public TrimOnDispose(int resolverSnapshot, int configSnapshot)
        {
            this.resolverSnapshot = resolverSnapshot;
            this.configSnapshot = configSnapshot;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;

            lock (syncLock)
            {
                if (resolvers.Count > resolverSnapshot)
                    resolvers.RemoveRange(resolverSnapshot, resolvers.Count - resolverSnapshot);
                if (configurators.Count > configSnapshot)
                    configurators.RemoveRange(configSnapshot, configurators.Count - configSnapshot);
                cached = null;
            }
        }
    }
}
