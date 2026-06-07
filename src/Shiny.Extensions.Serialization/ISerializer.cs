namespace Shiny;


/// <summary>
/// Provides serialization and deserialization of objects to and from strings, UTF-8 bytes, and streams.
/// Only generic overloads are exposed so the type is statically visible to the trim/AOT analyzer —
/// runtime-typed (object + Type) overloads were intentionally omitted because callers like
/// <c>Serialize(obj, obj.GetType())</c> cannot be statically verified to have a registered
/// <c>JsonTypeInfo</c>. For polymorphism, use STJ's <c>[JsonDerivedType]</c> on the host context.
/// </summary>
public interface ISerializer
{
    /// <summary>Deserializes a string to the specified type.</summary>
    T Deserialize<T>(string value);

    /// <summary>Serializes an object to a string representation.</summary>
    string Serialize<T>(T value);


    // ---------- UTF-8 byte overloads ----------

    /// <summary>Serializes <paramref name="value"/> to a UTF-8 byte array.</summary>
    byte[] SerializeToUtf8Bytes<T>(T value);

    /// <summary>Deserializes a UTF-8 byte span to <typeparamref name="T"/>.</summary>
    T Deserialize<T>(ReadOnlySpan<byte> utf8Json);


    // ---------- Stream overloads ----------

    /// <summary>Asynchronously serializes <paramref name="value"/> as UTF-8 to <paramref name="stream"/>.</summary>
    Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously deserializes a UTF-8 stream to <typeparamref name="T"/>.</summary>
    ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deserializes a UTF-8 JSON array stream as an <see cref="IAsyncEnumerable{T}"/>,
    /// yielding elements as they parse.
    /// </summary>
    IAsyncEnumerable<T?> DeserializeAsyncEnumerable<T>(Stream stream, CancellationToken cancellationToken = default);
}
