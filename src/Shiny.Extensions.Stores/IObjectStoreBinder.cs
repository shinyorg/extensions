using System.ComponentModel;

namespace Shiny.Extensions.Stores;


/// <summary>
/// Binds <see cref="INotifyPropertyChanged"/> instances to an <see cref="IKeyValueStore"/>,
/// hydrating their properties on bind and persisting them on property changes.
/// </summary>
public interface IObjectStoreBinder
{
    /// <summary>
    /// Attempts to bind an object to a keyed store. If <paramref name="storeKey"/> is null,
    /// the binder looks at <see cref="ObjectStoreBinderAttribute"/> on the type; if neither is
    /// present, the default (unkeyed) <see cref="IKeyValueStore"/> is used.
    /// </summary>
    void Bind(INotifyPropertyChanged npc, object? storeKey = null);


    /// <summary>
    /// Binds an object directly to a given store.
    /// </summary>
    void Bind(INotifyPropertyChanged npc, IKeyValueStore store);


    /// <summary>
    /// Unbinds an object from whatever store it was bound to.
    /// </summary>
    void UnBind(INotifyPropertyChanged npc);


    /// <summary>
    /// Unbinds any existing instances.
    /// </summary>
    void UnBindAll();
}
