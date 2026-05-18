namespace Shiny.Extensions.Stores;


/// <summary>
/// Declares which keyed <see cref="IKeyValueStore"/> a class should be bound to by <see cref="IObjectStoreBinder"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ObjectStoreBinderAttribute(string storeKey) : Attribute
{
    /// <summary>
    /// The DI service key of the <see cref="IKeyValueStore"/> to bind to.
    /// Typically a value from <see cref="StoreKeys"/>.
    /// </summary>
    public string StoreKey => storeKey;
}
