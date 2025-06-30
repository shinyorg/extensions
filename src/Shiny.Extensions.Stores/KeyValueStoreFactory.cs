namespace Shiny.Extensions.Stores;


public interface IKeyValueStoreFactory
{
    string[] AvailableStores { get; }
    bool HasStore(string aliasName);
    IKeyValueStore GetStore(string aliasName);
    IKeyValueStore DefaultStore { get; }
    void SetDefaultStore(string aliasName);
}


public class KeyValueStoreFactory(IEnumerable<IKeyValueStore> keyStores) : IKeyValueStoreFactory
{
    public static string DefaultStoreName { get; set; } = "settings";


    IKeyValueStore? defaultStore;
    public IKeyValueStore DefaultStore => this.defaultStore ??= this.GetStore(DefaultStoreName);

    public void SetDefaultStore(string aliasName)
        => this.defaultStore = this.GetStore(aliasName);

    public string[] AvailableStores
        => keyStores.Select(x => x.Alias).ToArray();

    public bool HasStore(string aliasName)
        => keyStores.Any(x => x.Alias.Equals(aliasName, StringComparison.InvariantCultureIgnoreCase));

    public IKeyValueStore GetStore(string aliasName) =>
        keyStores.FirstOrDefault(x => x.Alias.Equals(aliasName, StringComparison.InvariantCultureIgnoreCase)) ??
        throw new ArgumentException("No key/value store named " + aliasName);
}
