using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Shiny.Extensions.Stores.Infrastructure;


public class ObjectStoreBinder(
    IKeyValueStoreFactory factory,
    ILogger<ObjectStoreBinder>? logger = null
) : IObjectStoreBinder, IDisposable
{
    readonly object syncLock = new();
    readonly Dictionary<object, IKeyValueStore> bindings = new();
    readonly List<INotifyPropertyChanged> boundObjects = new();


    public void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null)
    {
        IKeyValueStore? store = null;

        if (!String.IsNullOrWhiteSpace(keyValueStoreAlias))
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
            if (reflector.Properties.Count(x => x.HasSetter) == 0)
            {
                logger?.LogDebug("Skipped Binding {ObjType} (no get/set properties) on Alias {Alias}", npc.GetType()!.FullName!, store.Alias);
            }
            else
            {
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
                        catch (Exception exception)
                        {
                            logger?.LogError(
                                exception, 
                                "Failed to bind {Type}.{PropertyName} with Value '{Value}",
                                npc.GetType().FullName!, 
                                prop.Name, value
                            );
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
                logger?.LogDebug("NPC Service {Type} has been bound to store '{Alias}", 
                    npc.GetType().FullName!,
                    store.Alias
                );
            }
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "NPC Service {Type} failed to been bound to store '{Alias}", npc.GetType().FullName!, store?.Alias ?? "Unknown");
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
            logger?.LogDebug("Null sender");
            return;
        }

        if (String.IsNullOrWhiteSpace(args.PropertyName))
        {
            logger?.LogDebug("Property name is null or empty - binding all properties for {Type}", sender.GetType().FullName);
            this.BindAllProperties(sender);
        }
        else
        {
            logger?.LogDebug("Binding property {PropertyName} to {Type}", args.PropertyName, sender.GetType().FullName);
            this.BindSpecificProperty(sender, args.PropertyName);
        }
    }

    
    void BindAllProperties(object sender)
    {
        lock (this.syncLock)
        {
            var reflector = sender.GetReflector(true)!;
            
            if (!this.bindings.TryGetValue(sender, out var binding))
                throw new ArgumentException("No key/value store found for current binding object - " + sender.GetType().FullName);

            foreach (var prop in reflector.Properties)
            {
                if (prop.HasSetter)
                {
                    var key = GetBindingKey(sender.GetType(), prop.Name);
                    var value = reflector[prop.Name];
                    binding.SetOrRemove(key, value);
                }
            }
        }
    }
    

    void BindSpecificProperty(object sender, string propertyName)
    {
        var reflector = sender.GetReflector(true)!;
        var prop = reflector.TryGetPropertyInfo(propertyName);
        
        if (prop == null)
        {
            logger?.LogWarning(
                "Property '{PropertyName}' not found on {Type}", 
                propertyName, 
                sender.GetType().FullName
            );
        }
        else
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
