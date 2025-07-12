using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.DependencyInjection;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ServiceAttribute(ServiceLifetime lifetime) : Attribute
{
    public string? KeyedName { get; set; }
    public string? Category { get; set; }
}