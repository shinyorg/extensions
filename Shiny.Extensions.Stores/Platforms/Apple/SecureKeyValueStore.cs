using Security;

namespace Shiny.Extensions.Stores;


public class SecureKeyValueStore(ISerializer serializer) : IKeyValueStore
{
    readonly object syncLock = new();

    public string Service { get; set; }//= $"{platform.AppIdentifier}.secure";
    public SecAccessible DefaultAccessible { get; set; } = SecAccessible.Always;


    public string Alias => "secure";
    public bool IsReadOnly => false;
    

    public void Clear()
    {
        lock (this.syncLock)
        {
            using var query = new SecRecord(SecKind.GenericPassword) { Service = this.Service };
            SecKeyChain.Remove(query);
        }
    }


    public bool Contains(string key)
    {
        lock (this.syncLock)
        {
            using var record = this.GetRecord(key);
            using var match = SecKeyChain.QueryAsRecord(record, out var result);

            return result == SecStatusCode.Success;
        }
    }


    public object? Get(Type type, string key)
    {
        lock (this.syncLock)
        {
            object? result = null;
            using var record = this.GetRecord(key);
            using var match = SecKeyChain.QueryAsRecord(record, out var resultCode);

            if (resultCode == SecStatusCode.Success)
            {
                var value = NSString.FromData(match!.ValueData!, NSStringEncoding.UTF8);
                result = serializer.Deserialize(type, value);
            }
            return result;
        }
    }


    public bool Remove(string key)
    {
        lock (this.syncLock)
        {
            var removed = false;
            using var record = this.GetRecord(key);
            using var match = SecKeyChain.QueryAsRecord(record, out var result);
            if (result == SecStatusCode.Success)
            {
                result = SecKeyChain.Remove(record);
                if (result != SecStatusCode.Success)
                    throw new ArgumentException("Error removing secure value - " + result);

                removed = true;
            }
            return removed;
        }
    }


    public void Set(string key, object value)
    {
        this.Remove(key);

        lock (this.syncLock)
        {
            var content = serializer.Serialize(value);
            var record = new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = this.Service,
                Label = key,
                Accessible = this.DefaultAccessible,
                ValueData = NSData.FromString(content, NSStringEncoding.UTF8),
            };
            var result = SecKeyChain.Add(record);

            if (result != SecStatusCode.Success)
                throw new ArgumentException("Failed to add secure value - " + result);
        }
    }


    protected virtual SecRecord GetRecord(string key) => new SecRecord(SecKind.GenericPassword)
    {
        Account = key,
        Service = this.Service
    };
}