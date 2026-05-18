using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Extensions.Stores.Infrastructure;


public class ObjectStoreBinder(
    IServiceProvider services,
    ISerializer serializer,
    ILogger<ObjectStoreBinder>? logger = null
) : IObjectStoreBinder, IDisposable
{
    readonly object syncLock = new();
    readonly Dictionary<object, IKeyValueStore> bindings = new();
    readonly List<INotifyPropertyChanged> boundObjects = new();


    public void Bind(INotifyPropertyChanged npc, object? storeKey = null)
    {
        if (storeKey == null)
        {
            var attrKey = npc.GetType()
                .GetCustomAttribute<ObjectStoreBinderAttribute>()?
                .StoreKey;

            storeKey = attrKey;
        }

        IKeyValueStore store;
        if (storeKey != null)
        {
            store = services.GetRequiredKeyedService<IKeyValueStore>(storeKey);
        }
        else
        {
            store = services.GetService<IKeyValueStore>()
                ?? throw new InvalidOperationException(
                    "No default IKeyValueStore registered. Call AddShinyStores or pass a storeKey."
                );
        }

        this.Bind(npc, store);
    }


    public void Bind(INotifyPropertyChanged npc, IKeyValueStore store)
    {
        try
        {
            var reflector = npc.GetReflector(true)!;
            if (reflector.Properties.Count(x => x.HasSetter) == 0)
            {
                logger?.LogDebug("Skipped Binding {ObjType} (no get/set properties)", npc.GetType()!.FullName!);
                return;
            }

            foreach (var prop in reflector.Properties)
            {
                if (!prop.HasSetter)
                    continue;

                var key = GetBindingKey(npc.GetType(), prop.Name);
                if (!store.Contains(key))
                    continue;

                try
                {
                    var value = GetTyped(store, key, prop.Type, serializer);
                    if (value != null)
                        reflector.SetValue(prop.Name, value);
                }
                catch (Exception exception)
                {
                    logger?.LogError(
                        exception,
                        "Failed to bind {Type}.{PropertyName}",
                        npc.GetType().FullName!,
                        prop.Name
                    );
                }
            }

            lock (this.syncLock)
            {
                this.boundObjects.Add(npc);
                this.bindings.Add(npc, store);
            }

            npc.PropertyChanged += this.OnPropertyChanged;
            logger?.LogDebug("NPC Service {Type} has been bound", npc.GetType().FullName!);
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "NPC Service {Type} failed to bind", npc.GetType().FullName!);
        }
    }


    public virtual void UnBind(INotifyPropertyChanged obj)
    {
        obj.PropertyChanged -= this.OnPropertyChanged;
        lock (this.syncLock)
        {
            this.boundObjects.Remove(obj);
            this.bindings.Remove(obj);
        }
    }


    public virtual void UnBindAll()
    {
        lock (this.syncLock)
        {
            foreach (var boundObj in this.boundObjects)
                boundObj.PropertyChanged -= this.OnPropertyChanged;

            this.boundObjects.Clear();
            this.bindings.Clear();
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
                if (!prop.HasSetter)
                    continue;

                var key = GetBindingKey(sender.GetType(), prop.Name);
                var value = reflector[prop.Name];
                SetTyped(binding, key, prop.Type, value, serializer);
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
            return;
        }

        var key = GetBindingKey(sender.GetType(), prop.Name);
        var value = reflector[prop.Name];

        lock (this.syncLock)
        {
            if (!this.bindings.TryGetValue(sender, out var binding))
                throw new ArgumentException("No key/value store found for current binding object - " + sender.GetType().FullName);

            SetTyped(binding, key, prop.Type, value, serializer);
        }
    }


    public void Dispose() => this.UnBindAll();


    // ----- AOT-safe runtime typed dispatch -----

    static void SetTyped(IKeyValueStore store, string key, Type type, object? value, ISerializer serializer)
    {
        if (value == null || IsDefault(type, value))
        {
            store.Remove(key);
            return;
        }

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean: store.Set(key, (bool)value); break;
            case TypeCode.SByte:   store.Set(key, (sbyte)value); break;
            case TypeCode.Byte:    store.Set(key, (byte)value); break;
            case TypeCode.Int16:   store.Set(key, (short)value); break;
            case TypeCode.UInt16:  store.Set(key, (ushort)value); break;
            case TypeCode.Int32:   store.Set(key, (int)value); break;
            case TypeCode.UInt32:  store.Set(key, (uint)value); break;
            case TypeCode.Int64:   store.Set(key, (long)value); break;
            case TypeCode.UInt64:  store.Set(key, (ulong)value); break;
            case TypeCode.Single:  store.Set(key, (float)value); break;
            case TypeCode.Double:  store.Set(key, (double)value); break;
            case TypeCode.Decimal: store.Set(key, (decimal)value); break;
            case TypeCode.Char:    store.Set(key, (char)value); break;
            case TypeCode.String:  store.Set(key, (string)value); break;
            case TypeCode.DateTime: store.Set(key, (DateTime)value); break;
            default:
                store.Set(key, serializer.Serialize(value, type));
                break;
        }
    }


    static object? GetTyped(IKeyValueStore store, string key, Type type, ISerializer serializer)
    {
        if (!store.Contains(key))
            return null;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean: return store.Get<bool>(key);
            case TypeCode.SByte:   return store.Get<sbyte>(key);
            case TypeCode.Byte:    return store.Get<byte>(key);
            case TypeCode.Int16:   return store.Get<short>(key);
            case TypeCode.UInt16:  return store.Get<ushort>(key);
            case TypeCode.Int32:   return store.Get<int>(key);
            case TypeCode.UInt32:  return store.Get<uint>(key);
            case TypeCode.Int64:   return store.Get<long>(key);
            case TypeCode.UInt64:  return store.Get<ulong>(key);
            case TypeCode.Single:  return store.Get<float>(key);
            case TypeCode.Double:  return store.Get<double>(key);
            case TypeCode.Decimal: return store.Get<decimal>(key);
            case TypeCode.Char:    return store.Get<char>(key);
            case TypeCode.String:  return store.Get<string>(key);
            case TypeCode.DateTime: return store.Get<DateTime>(key);
            default:
                var json = store.Get<string>(key);
                return json == null ? null : serializer.Deserialize(type, json);
        }
    }


    static bool IsDefault(Type type, object value) => Type.GetTypeCode(type) switch
    {
        TypeCode.Boolean  => !(bool)value,
        TypeCode.SByte    => (sbyte)value == 0,
        TypeCode.Byte     => (byte)value == 0,
        TypeCode.Int16    => (short)value == 0,
        TypeCode.UInt16   => (ushort)value == 0,
        TypeCode.Int32    => (int)value == 0,
        TypeCode.UInt32   => (uint)value == 0,
        TypeCode.Int64    => (long)value == 0L,
        TypeCode.UInt64   => (ulong)value == 0UL,
        TypeCode.Single   => (float)value == 0f,
        TypeCode.Double   => (double)value == 0d,
        TypeCode.Decimal  => (decimal)value == 0m,
        TypeCode.Char     => (char)value == '\0',
        TypeCode.DateTime => (DateTime)value == default,
        _                 => false
    };
}
