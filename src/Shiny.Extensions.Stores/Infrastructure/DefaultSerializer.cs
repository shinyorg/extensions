using System.Text.Json;

namespace Shiny.Extensions.Stores.Infrastructure;


public class DefaultSerializer : ISerializer
{
    // [JsonSourceGenerationOptions(UseStringEnumConverter = true)]
    // source gen considerations
    // var options = new JsonSerializerOptions
    // {
    //     TypeInfoResolver = JsonTypeInfoResolver.Combine(ContextA.Default, ContextB.Default, ContextC.Default);
    // };
    public JsonSerializerOptions SerializerOptions { get; set; } = new();


    public T Deserialize<T>(string value) => (T)this.Deserialize(typeof(T), value);
    public object Deserialize(Type objectType, string value)
    {
        var result = JsonSerializer.Deserialize(value, objectType, this.SerializerOptions);
        return result!;
    }
    public string Serialize(object value) => JsonSerializer.Serialize(value, this.SerializerOptions);
}