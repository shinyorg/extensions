

using System.Reflection;

namespace Shiny.Extensions.Stores.Impl;


public class ObjectStoreBinder(
    IKeyValueStoreFactory factory 
    // ILogger<ObjectStoreBinder>? logger
) : IObjectStoreBinder, IDisposable
{
    readonly object syncLock = new();
    readonly Dictionary<object, IKeyValueStore> bindings = new();
    readonly List<INotifyPropertyChanged> boundObjects = new();


    public void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null)
    {
        IKeyValueStore? store = null;

        if (keyValueStoreAlias != null)
        {
            store = factory.GetStore(keyValueStoreAlias);
        }
        else
        {
            keyValueStoreAlias = npc
                .GetType()
                .GetCustomAttribute<ObjectStoreBinderAttribute>()?
                .StoreAlias;

            if (keyValueStoreAlias != null)
                store = factory.GetStore(keyValueStoreAlias); // error if attribute is bad
        }

        this.Bind(npc, store ?? factory.DefaultStore);
    }


    public void Bind(INotifyPropertyChanged npc, IKeyValueStore store)
    {
        try
        {
            var reflector = npc.GetReflector(true)!;
            if (reflector.Properties.Length == 0)
            {
                // logger?.BindInfo("Skipped (no get/set properties)", npc.GetType()!.FullName!, store.Alias);
                return;
            }

            foreach (var prop in reflector.Properties)
            {
                var key = GetBindingKey(npc.GetType(), prop.Name);

                if (store.Contains(key))
                {
                    var value = store.Get(prop.Type, key);
                    try
                    {
                        reflector.SetValue(prop.Name, value);
                    }
                    catch (Exception ex)
                    {
                        // logger?.PropertyBindError(ex, npc.GetType().FullName!, prop.Name);
                    }
                }
            }
            lock (this.syncLock)
            {
                // set these before npc hook
                this.boundObjects.Add(npc);
                this.bindings.Add(npc, store);
            }

            npc.PropertyChanged += this.OnPropertyChanged;
            // logger?.BindInfo("Success", npc.GetType().FullName!, store.Alias);
        }
        catch (Exception ex)
        {
            // logger?.BindError(ex, npc?.GetType().FullName ?? "Unknown", store.Alias);
        }
    }


    public virtual void UnBind(INotifyPropertyChanged obj)
    {
        obj.PropertyChanged -= this.OnPropertyChanged;
        lock (this.syncLock)
            this.boundObjects.Remove(obj);
    }


    public virtual void UnBindAll()
    {
        lock (this.syncLock)
        {
            foreach (var boundObj in this.boundObjects)
                boundObj.PropertyChanged -= this.OnPropertyChanged;

            this.boundObjects.Clear();
        }
    }
    

    public static string GetBindingKey(Type type, string propertyName)
        => $"{type.Namespace}.{type.Name}.{propertyName}";

    protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender == null)
        {
            // this.logger.LogDebug("Null sender");
            return;
        }

        var reflector = sender.GetReflector(true)!;

        var prop = reflector.TryGetPropertyInfo(args.PropertyName);
        if (prop != null)
        {
            var key = GetBindingKey(sender.GetType(), prop.Name);
            var value = reflector[prop.Name];

            lock (this.syncLock)
            {
                if (!this.bindings.TryGetValue(sender, out var binding))
                    throw new ArgumentException("No key/value store found for current binding object - " + sender.GetType().FullName);

                binding.SetOrRemove(key, value);
            }
        }
    }

    public void Dispose() => this.UnBindAll();
}
