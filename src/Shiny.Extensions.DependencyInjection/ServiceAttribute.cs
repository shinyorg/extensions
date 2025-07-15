using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.DependencyInjection;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ServiceAttribute(ServiceLifetime lifetime) : Attribute
{
    /// <summary>
    /// If your service implements multiple interfaces, you can specify the specific interface you want it implemented with instead
    /// </summary>
    public Type? Type { get; set; }
    
    /// <summary>
    /// The keyed service name to use for this service
    /// </summary>
    public string? KeyedName { get; set; }
    
    /// <summary>
    /// The category of the service to use for optional installation in the AddGeneratedServices method
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Will call the correspond TryAddLifetime method on the IServiceCollection unless multiple interfaces are detected
    /// </summary>
    public bool TryAdd { get; set; }
}


public class SingletonAttribute : ServiceAttribute
{
    public SingletonAttribute() : base(ServiceLifetime.Singleton) { }
}

public class ScopedAttribute : ServiceAttribute
{
    public ScopedAttribute() : base(ServiceLifetime.Scoped) { }
}
public class TransientAttribute : ServiceAttribute
{
    public TransientAttribute() : base(ServiceLifetime.Transient) { }
}