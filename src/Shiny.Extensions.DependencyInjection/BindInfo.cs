namespace Shiny;


/// <summary>
/// Compile-time metadata for a property bound to a Shiny key/value store via <see cref="BindAttribute"/>.
/// One instance is emitted per bound property into the generated nested <c>Binds</c> static class
/// on each bind type, plus an <c>All</c> array enumerating every bound property on the type.
/// </summary>
/// <param name="PropertyName">The CLR property name on the bind class.</param>
/// <param name="PropertyType">The CLR type of the property.</param>
/// <param name="StoreKey">The DI service key of the target store. <c>null</c> means the default store.</param>
/// <param name="StorageKey">The key used inside the store. Defaults to <see cref="PropertyName"/> unless overridden via <see cref="BindAttribute.Key"/>.</param>
public sealed record BindInfo(
    string PropertyName,
    global::System.Type PropertyType,
    string? StoreKey,
    string StorageKey
);
