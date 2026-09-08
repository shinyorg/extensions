using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Shiny.Extensions.Reflector.Infrastructure;


/// <summary>
/// The runtime reflection fallback used when a type has no source generated reflector.
/// </summary>
/// <remarks>
/// The reflected type is only known at runtime, so its properties cannot be discovered statically.
/// This type is not trim or AOT safe - apply <see cref="ReflectorAttribute"/> to the type instead.
/// </remarks>
[RequiresUnreferencedCode(TrimWarnings.TrueReflection)]
public class TrueReflectionReflectorClass(object obj) : IReflectorClass
{
    public object ReflectedObject => obj;


    ReflectedPropertyGeneratedInfo[]? props;
    PropertyGeneratedInfo[] RefProps => this.props ??= this.ReflectedObject
        .GetType()
        .GetProperties()
        .Where(x => x.GetMethod != null)
        .Select(x => new ReflectedPropertyGeneratedInfo(x))
        .ToArray();
    
    public PropertyGeneratedInfo[] Properties => this.RefProps.ToArray();
    
    public T? GetValue<T>(string key)
        => this[key] is T value ? value : default;

    public void SetValue<T>(string key, T? value)
        => this[key] = value;

    public object? this[string key]
    {
        get
        {
            var prop = this.RefProps
                .Cast<ReflectedPropertyGeneratedInfo>()
                .FirstOrDefault(x => x.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase));
            
            if (prop == null)
                throw new ArgumentException($"Property '{key}' not found on {this.ReflectedObject.GetType().Name}");
            
            var value = prop.Property.GetValue(this.ReflectedObject);
            return value;
        }
        set
        {
            var prop = this.RefProps
                .Cast<ReflectedPropertyGeneratedInfo>()
                .FirstOrDefault(x => x.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase));
            
            if (prop == null)
                throw new ArgumentException($"Property '{key}' not found on {this.ReflectedObject.GetType().Name}");
            
            prop.Property.SetValue(this.ReflectedObject, value);
        }
    }
}

public record ReflectedPropertyGeneratedInfo(
    PropertyInfo Property
) : PropertyGeneratedInfo(
    Property.Name, 
    Property.PropertyType, 
    Property.SetMethod != null && !Property
        .SetMethod
        .ReturnParameter
        .GetRequiredCustomModifiers()
        .Any(m => m.Name == "IsExternalInit")
);
