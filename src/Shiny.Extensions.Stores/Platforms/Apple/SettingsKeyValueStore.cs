namespace Shiny.Extensions.Stores;


public class SettingsKeyValueStore(ISerializer serializer) : IKeyValueStore
{
    public bool IsReadOnly => false;


    /// <inheritdoc />
    /// <remarks>
    /// Drops the app's whole persistent domain in one call. It used to walk
    /// <c>prefs.ToDictionary()</c> and remove every key it found — but that dictionary is the merged
    /// search list (see <see cref="Owns"/>), so it walked the user's language list, locale and
    /// every framework's registered defaults, and asked to remove each of them.
    /// </remarks>
    public void Clear() => this.Do(prefs =>
    {
        var domain = BundleId;
        if (domain == null)
        {
            foreach (var key in prefs.ToDictionary())
                prefs.RemoveObject(key.Key.ToString());

            return;
        }

        prefs.RemovePersistentDomain(domain);
    });


    public bool Contains(string key) => this.GetValue(prefs => this.Owns(prefs, key));


    public T? Get<T>(string key) => this.GetValue<T?>(prefs =>
    {
        if (!this.Owns(prefs, key))
            return default;

        // Enums are written via the default branch in Set (boxed enum values do not
        // match `case int i:`), so they land in NSUserDefaults as JSON strings. Route
        // them through the serializer to keep the read/write paths symmetric.
        if (typeof(T).IsEnum)
            return serializer.Deserialize<T>(prefs.StringForKey(key) ?? string.Empty);

        // Read back through the search list rather than out of the domain dictionary: the app's own
        // domain outranks the global and registration domains, so once Owns has said the key is ours
        // these return our value — and they keep the typed read path exactly as Set wrote it.
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Boolean => (T)(object)prefs.BoolForKey(key),
            TypeCode.Double  => (T)(object)prefs.DoubleForKey(key),
            TypeCode.Int32   => (T)(object)(int)prefs.IntForKey(key),
            TypeCode.Single  => (T)(object)prefs.FloatForKey(key),
            TypeCode.String  => (T)(object)(prefs.StringForKey(key) ?? string.Empty),
            _                => serializer.Deserialize<T>(prefs.StringForKey(key) ?? string.Empty)
        };
    });


    public bool Remove(string key) => this.GetValue(prefs =>
    {
        if (!this.Owns(prefs, key))
            return false;

        prefs.RemoveObject(key);
        return true;
    });


    public void Set<T>(string key, T value) => this.Do(prefs =>
    {
        switch (value)
        {
            case bool b:   prefs.SetBool(b, key); break;
            case double d: prefs.SetDouble(d, key); break;
            case int i:    prefs.SetInt(i, key); break;
            case float f:  prefs.SetFloat(f, key); break;
            case string s: prefs.SetString(s, key); break;
            case null:     prefs.RemoveObject(key); break;
            default:
                prefs.SetString(serializer.Serialize<T>(value), key);
                break;
        }
    });


    static string? BundleId => NSBundle.MainBundle?.BundleIdentifier;


    /// <summary>
    /// Whether <b>this app</b> has a value stored under <paramref name="key"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b><c>NSUserDefaults.StandardUserDefaults</c> is a search list, not this app's storage.</b> It
    /// resolves a key through the argument domain, the app's own persistent domain, the global domain,
    /// the language domains and the registration domain in turn — so <c>ValueForKey</c>, which is what
    /// this used to ask, answers <i>"does anything in this process know this key"</i> rather than
    /// <i>"did this store write this key"</i>. Those are the same answer only in an app that shares its
    /// process with nothing.
    /// </para>
    /// <para>
    /// The difference is not academic and it fails silently. Every framework linked into the app may call
    /// <c>registerDefaults:</c>, and the global domain carries the user's own settings; a bound property
    /// with an ordinary name — <c>AutoRecord</c>, <c>UseMetric</c>, <c>Enabled</c> — therefore read back
    /// as <b>present</b> having never been written. <c>StoreExtensions.Get(store, key, defaultValue)</c>
    /// applies the caller's default only when <see cref="Contains"/> is false, so the foreign value won
    /// and a <c>[Bind(Default = true)]</c> property came back false for the life of the install, with
    /// nothing logged and nothing on disk to explain it.
    /// </para>
    /// <para>
    /// The persistent domain is the app's own plist and nothing else, which is exactly the question the
    /// interface asks. Reads stay on the typed accessors above — the app domain outranks the global and
    /// registration domains, so a key this store owns resolves to this store's value.
    /// </para>
    /// <para>
    /// A null bundle identifier (a unit-test host, an unbundled executable) has no persistent domain to
    /// consult, so the old search-list behaviour stands there rather than reporting an empty store.
    /// </para>
    /// </remarks>
    bool Owns(NSUserDefaults prefs, string key)
    {
        var domain = BundleId;
        if (domain == null)
            return prefs.ValueForKey(new NSString(key)) != null;

        using var own = prefs.PersistentDomainForName(domain);
        return own?.ContainsKey(new NSString(key)) == true;
    }


    readonly object syncLock = new();

    protected virtual T GetValue<T>(Func<NSUserDefaults, T> getter)
    {
        lock (this.syncLock)
        {
            using var native = NSUserDefaults.StandardUserDefaults;
            return getter(native);
        }
    }

    protected virtual void Do(Action<NSUserDefaults> action)
    {
        lock (this.syncLock)
        {
            using var native = NSUserDefaults.StandardUserDefaults;
            action(native);
            native.Synchronize();
        }
    }
}
