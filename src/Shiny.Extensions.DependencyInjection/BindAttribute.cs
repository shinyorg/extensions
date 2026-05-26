namespace Shiny;


/// <summary>
/// Marks a partial property for source-generated binding to a Shiny key/value store.
/// The generator emits getter/setter bodies that read from and write to the store accessed via
/// <c>Shiny.Stores</c> (from the <c>Shiny.Extensions.Stores</c> package).
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public class BindAttribute : Attribute
{
    /// <summary>Default ctor — binds to the default store (<c>Shiny.Extensions.Stores.StoreKeys.Default</c>).</summary>
    public BindAttribute() { }

    /// <summary>Binds to the keyed store identified by <paramref name="storeKey"/>.</summary>
    /// <param name="storeKey">DI service key of the target <c>IKeyValueStore</c>. Pass <c>StoreKeys.Secure</c> for the secure store.</param>
    public BindAttribute(string storeKey) { this.StoreKey = storeKey; }

    /// <summary>The DI service key of the target store. Null means the default store.</summary>
    public string? StoreKey { get; }

    /// <summary>Override the storage key. Defaults to the property name.</summary>
    public string? Key { get; set; }

    /// <summary>
    /// Optional default value returned by the generated getter when the store does not contain a value
    /// for this key. Must be a compile-time constant (primitive, string, enum, or <c>typeof</c>) whose
    /// type is implicitly convertible to the property type — the source generator verifies this and
    /// emits diagnostic <c>DI002</c> on mismatch.
    /// </summary>
    public object? Default { get; set; }
}
