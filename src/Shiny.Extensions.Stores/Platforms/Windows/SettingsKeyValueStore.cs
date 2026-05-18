using Windows.Storage;

namespace Shiny.Extensions.Stores;


public class SettingsKeyValueStore(ISerializer serializer) : IKeyValueStore
{
    public string? ContainerName { get; set; }

    public bool IsReadOnly => false;
    public void Clear() => this.Container.Values.Clear();
    public bool Contains(string key) => this.Container.Values.ContainsKey(key);


    public T? Get<T>(string key)
    {
        if (!this.Contains(key))
            return default;

        var raw = this.Container.Values[key];

        if (raw is T direct)
            return direct;

        if (raw is string s)
            return serializer.Deserialize<T>(s);

        return default;
    }


    public bool Remove(string key) => this.Container.Values.Remove(key);


    public void Set<T>(string key, T value)
    {
        if (value is null)
        {
            this.Remove(key);
            return;
        }

        object stored = value switch
        {
            bool   => value!,
            int    => value!,
            long   => value!,
            double => value!,
            float  => value!,
            string => value!,
            byte[] => value!,
            _      => serializer.Serialize<T>(value)
        };

        if (this.Contains(key))
            this.Container.Values[key] = stored;
        else
            this.Container.Values.Add(key, stored);
    }


    ApplicationDataContainer? container;
    protected virtual ApplicationDataContainer Container
    {
        get
        {
            this.ContainerName ??= "shiny";
            this.container ??= ApplicationData.Current.LocalSettings.CreateContainer(this.ContainerName, ApplicationDataCreateDisposition.Always);
            return this.container;
        }
    }
}
