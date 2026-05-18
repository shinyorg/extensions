namespace Shiny.Extensions.Stores;


/// <summary>
/// Provides serialization and deserialization of objects to and from strings.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Deserializes a string to the specified type.
    /// </summary>
    T Deserialize<T>(string value);

    /// <summary>
    /// Serializes an object to a string representation.
    /// </summary>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes a string to the specified runtime type.
    /// Used by <see cref="IObjectStoreBinder"/> for property-typed binding.
    /// </summary>
    object? Deserialize(Type type, string value);

    /// <summary>
    /// Serializes a runtime-typed value. The declared type is used to select the JsonTypeInfo;
    /// when null, <c>value.GetType()</c> is used.
    /// </summary>
    string Serialize(object value, Type? declaredType = null);
}
