using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny;


public static class DIExtensions
{
    /// <summary>
    /// This method registers a singleton service for the specified implementation type against all of the interfaces it implements.
    /// All instances returned will be the same instance for all interfaces.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="keyName">(optional) Registers all interfaces with keyname as well</param>
    /// <typeparam name="TImpl"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddSingletonAsImplementedInterfaces<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TImpl
    >(this IServiceCollection services, string? keyName = null)
        where TImpl : class
    {
        if (keyName == null)
            services.AddSingleton<TImpl>();
        else
            services.AddKeyedSingleton<TImpl>(keyName);

        var interfaces = typeof(TImpl)
            .GetInterfaces()
            .Where(x => x != typeof(IDisposable));

        foreach (var interfaceType in interfaces)
        {
            if (keyName == null)
                services.AddSingleton(interfaceType, sp => sp.GetRequiredService<TImpl>());
            else
                services.AddKeyedSingleton(interfaceType, keyName, (sp, key) => sp.GetRequiredKeyedService<TImpl>(key));
        }

        return services;
    }


    /// <summary>
    /// This method registers a scoped service for the specified implementation type against all of the interfaces it implements.
    /// All instances returned will be the same instance for all interfaces within the same lifecycle scope.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="keyName">(optional) Registers all interfaces with keyname as well</param>
    /// <typeparam name="TImpl"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddScopedAsImplementedInterfaces<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TImpl
    >(this IServiceCollection services, string? keyName = null)
        where TImpl : class
    {
        if (keyName == null)
            services.AddScoped<TImpl>();
        else
            services.AddKeyedScoped<TImpl>(keyName);

        var interfaces = typeof(TImpl)
            .GetInterfaces()
            .Where(x => x != typeof(IDisposable));

        foreach (var interfaceType in interfaces)
        {
            if (keyName == null)
                services.AddScoped(interfaceType, sp => sp.GetRequiredService<TImpl>());
            else
                services.AddKeyedScoped(interfaceType, keyName, (sp, key) => sp.GetRequiredKeyedService<TImpl>(key));
        }

        return services;
    }

    /// <summary>
    /// Checks if a service collection has a service registered for the specified type
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static bool HasService<TService>(this IServiceCollection services)
        => services.HasService(typeof(TService));

    /// <summary>
    /// Checks if a service collection has a service registered for the specified type
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    public static bool HasService(this IServiceCollection services, Type serviceType)
        => services.Any(x => x.ServiceType == serviceType);

    /// <summary>
    /// Checks if a service collection has an implementation registered for the specified type
    /// </summary>
    /// <typeparam name="TImpl"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static bool HasImplementation<TImpl>(this IServiceCollection services)
        => services.HasImplementation(typeof(TImpl));

    /// <summary>
    /// Checks if a service collection has an implementation registered for the specified type
    /// </summary>
    /// <param name="services"></param>
    /// <param name="implementationType"></param>
    /// <returns></returns>
    public static bool HasImplementation(this IServiceCollection services, Type implementationType)
        => services.Any(x => x.ServiceKey == null && x.ImplementationType == implementationType);

    /// <summary>
    /// Lazily resolves a service - helps in prevent resolve loops with delegates/services internal to Shiny
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="required"></param>
    /// <returns></returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2091:UnrecognizedReflectionPattern",
        Justification = "Lazy<T> is always constructed with an explicit value factory; the default-constructor requirement on T is not exercised."
    )]
    public static Lazy<T> GetLazyService<T>(this IServiceProvider services, bool required = false) where T : notnull
        => new(() => required ? services.GetRequiredService<T>() : services.GetService<T>()!);

    /// <summary>
    /// Chains a hook onto the most recently registered service. The hook fires after the underlying
    /// factory produces the instance, before it is returned to the caller.
    /// </summary>
    /// <remarks>
    /// Fires once per factory invocation, which depends on lifetime:
    /// Singleton -> once total; Scoped -> once per scope; Transient -> every resolve.
    /// <para/>
    /// AOT requirement: the last descriptor must have a factory (<c>ImplementationFactory</c> or
    /// <c>KeyedImplementationFactory</c>). Type-based registrations like
    /// <c>services.AddSingleton&lt;TService, TImpl&gt;()</c> are rejected, since wrapping them would
    /// require runtime reflection to construct <c>TImpl</c>. Register with a factory instead:
    /// <c>services.AddSingleton&lt;TService&gt;(sp =&gt; new TImpl(...))</c>.
    /// <para/>
    /// Throws if the collection is empty, the last descriptor's <c>ServiceType</c> is not assignable
    /// to <typeparamref name="TService"/>, or the last descriptor was registered with a type or
    /// pre-built instance instead of a factory.
    /// </remarks>
    public static IServiceCollection OnResolved<TService>(
        this IServiceCollection services,
        Action<TService, IServiceProvider> hook
    ) where TService : class
    {
        if (services.Count == 0)
            throw new InvalidOperationException("OnResolved requires a prior service registration to chain onto.");

        var last = services[^1];
        if (!typeof(TService).IsAssignableFrom(last.ServiceType))
            throw new InvalidOperationException(
                $"OnResolved<{typeof(TService).Name}> cannot chain onto a registration for '{last.ServiceType}'."
            );

        return services.WrapLast((instance, sp) =>
        {
            hook((TService)instance, sp);
            return instance;
        });
    }


    /// <summary>
    /// Chains a hook onto the most recently registered service. Overload for cases where the hook
    /// does not need the <see cref="IServiceProvider"/> - e.g. calling an initialize method on the
    /// resolved instance.
    /// </summary>
    /// <remarks>
    /// See <see cref="OnResolved{TService}(IServiceCollection, Action{TService, IServiceProvider})"/>
    /// for lifetime, AOT, and validation behavior.
    /// </remarks>
    public static IServiceCollection OnResolved<TService>(
        this IServiceCollection services,
        Action<TService> hook
    ) where TService : class
        => services.OnResolved<TService>((instance, _) => hook(instance));


    static IServiceCollection WrapLast(
        this IServiceCollection services,
        Func<object, IServiceProvider, object> hook
    )
    {
        var idx = services.Count - 1;
        var d = services[idx];

        if (d.IsKeyedService)
        {
            var factory = d.KeyedImplementationFactory
                ?? throw new InvalidOperationException(
                    $"Cannot chain onto keyed registration for '{d.ServiceType}': only factory-based registrations are supported (AOT). Register via AddKeyedSingleton/Scoped/Transient with a factory delegate."
                );

            services[idx] = new ServiceDescriptor(
                d.ServiceType,
                d.ServiceKey,
                (sp, key) => hook(factory(sp, key), sp),
                d.Lifetime
            );
        }
        else
        {
            var factory = d.ImplementationFactory
                ?? throw new InvalidOperationException(
                    $"Cannot chain onto registration for '{d.ServiceType}': only factory-based registrations are supported (AOT). Register via AddSingleton/Scoped/Transient with a factory delegate, e.g. services.AddSingleton<TService>(sp => new TImpl(...))."
                );

            services[idx] = new ServiceDescriptor(
                d.ServiceType,
                sp => hook(factory(sp), sp),
                d.Lifetime
            );
        }

        return services;
    }
}
