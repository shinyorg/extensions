using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.DependencyInjection;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class ServiceAttribute(ServiceLifetime lifetime, string? KeyedName = null, string? Category = null) : Attribute;