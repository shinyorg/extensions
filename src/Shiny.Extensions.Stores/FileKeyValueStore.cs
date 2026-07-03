using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.Extensions.Stores;


/// <summary>
/// A persistent, file-backed <see cref="IKeyValueStore"/> used on desktop targets that resolve
/// the base <c>net10.0</c> asset (plain macOS, Linux, and unpackaged Windows) where no
/// platform-native settings store is available. Values are held in memory and flushed to a
/// single JSON file on every mutation, so state survives process restarts (unlike
/// <see cref="MemoryKeyValueStore"/>).
///
/// Primitives, <c>byte[]</c>, and enums are stored using the same type handling as the Apple
/// <c>SettingsKeyValueStore</c>; any other type is routed through the shared
/// <see cref="ISerializer"/>, so the caller is responsible for registering its
/// <c>JsonTypeInfo</c> (via <c>[ShinyJsonInclude]</c>/<c>services.AddJsonContext(...)</c>) exactly
/// as on iOS/Android.
///
/// The on-disk container is a <c>Dictionary&lt;string,string&gt;</c> serialized through a local
/// source-generated context (<see cref="FileStoreJsonContext"/>) — never through
/// <see cref="ISerializer"/>, which has no unregistered-type fallback — keeping the store AOT/trim safe.
/// </summary>
public class FileKeyValueStore : IKeyValueStore
{
    readonly ISerializer serializer;
    readonly string filePath;
    readonly object syncLock = new();
    readonly Dictionary<string, string> values;


    public FileKeyValueStore(string filePath, ISerializer serializer)
    {
        this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.values = Load(filePath);
    }


    public bool IsReadOnly => false;


    public bool Contains(string key)
    {
        lock (this.syncLock)
            return this.values.ContainsKey(key);
    }


    public T? Get<T>(string key)
    {
        lock (this.syncLock)
        {
            if (!this.values.TryGetValue(key, out var raw))
                return default;

            return Read<T>(raw);
        }
    }


    public void Set<T>(string key, T value)
    {
        lock (this.syncLock)
        {
            if (value is null)
            {
                if (this.values.Remove(key))
                    this.Flush();
                return;
            }

            this.values[key] = this.Write(value);
            this.Flush();
        }
    }


    public bool Remove(string key)
    {
        lock (this.syncLock)
        {
            if (!this.values.Remove(key))
                return false;

            this.Flush();
            return true;
        }
    }


    public void Clear()
    {
        lock (this.syncLock)
        {
            if (this.values.Count == 0)
                return;

            this.values.Clear();
            this.Flush();
        }
    }


    T? Read<T>(string raw)
    {
        if (typeof(T) == typeof(byte[]))
            return (T)(object)Convert.FromBase64String(raw);

        if (typeof(T).IsEnum)
            return this.serializer.Deserialize<T>(raw);

        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Boolean => (T)(object)Boolean.Parse(raw),
            TypeCode.Int32   => (T)(object)Int32.Parse(raw, CultureInfo.InvariantCulture),
            TypeCode.Int64   => (T)(object)Int64.Parse(raw, CultureInfo.InvariantCulture),
            TypeCode.Double  => (T)(object)Double.Parse(raw, CultureInfo.InvariantCulture),
            TypeCode.Single  => (T)(object)Single.Parse(raw, CultureInfo.InvariantCulture),
            TypeCode.String  => (T)(object)raw,
            _                => this.serializer.Deserialize<T>(raw)
        };
    }


    string Write<T>(T value) => value switch
    {
        byte[] bytes => Convert.ToBase64String(bytes),
        bool b       => b ? "true" : "false",
        int i        => i.ToString(CultureInfo.InvariantCulture),
        long l       => l.ToString(CultureInfo.InvariantCulture),
        double d     => d.ToString(CultureInfo.InvariantCulture),
        float f      => f.ToString(CultureInfo.InvariantCulture),
        string s     => s,
        // Enums (boxed values do not match the primitive cases) and complex types go through
        // the serializer, keeping the read/write paths symmetric with the Apple store.
        _            => this.serializer.Serialize<T>(value)
    };


    void Flush()
    {
        var json = JsonSerializer.Serialize(this.values, FileStoreJsonContext.Default.DictionaryStringString);

        var dir = Path.GetDirectoryName(this.filePath);
        if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Write to a temp file then atomically replace so a crash mid-write can't corrupt the store.
        var tmp = this.filePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, this.filePath, overwrite: true);
    }


    static Dictionary<string, string> Load(string filePath)
    {
        if (!File.Exists(filePath))
            return new();

        try
        {
            var text = File.ReadAllText(filePath);
            if (String.IsNullOrWhiteSpace(text))
                return new();

            return JsonSerializer.Deserialize(text, FileStoreJsonContext.Default.DictionaryStringString) ?? new();
        }
        catch (Exception)
        {
            // Corrupt/unreadable file — start clean rather than crashing the app on first access.
            // The next write replaces it.
            return new();
        }
    }
}


[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class FileStoreJsonContext : JsonSerializerContext;
