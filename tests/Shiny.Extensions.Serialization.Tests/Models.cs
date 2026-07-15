namespace Shiny.Extensions.Serialization.Tests;


// ---------- Element types ----------

[ShinyJsonInclude]
public partial class AutoType
{
    public string Title { get; set; } = "";
    public DateTimeOffset Created { get; set; }
}


public class SingleOnlyType
{
    public string Value { get; set; } = "";
}


public class ForeignType
{
    public string Foreign { get; set; } = "";
    public int Number { get; set; }
}


// BoxedInt uses an inline JsonConverter. Element JsonTypeInfo comes from AppJsonContext below;
// collection metadata for List<BoxedInt> etc. comes from the generator-emitted resolver.
[ShinyJsonInclude]
[JsonConverter(typeof(BoxedIntConverter))]
public partial class BoxedInt
{
    public int Value { get; set; }
}


public sealed class BoxedIntConverter : JsonConverter<BoxedInt>
{
    public override BoxedInt? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return new BoxedInt { Value = reader.GetInt32() };
    }

    public override void Write(Utf8JsonWriter writer, BoxedInt value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}


// Never registered anywhere — used to verify the unknown-type error.
public class UnknownType
{
    public string Anything { get; set; } = "";
}


// ---------- The user-declared host context. [ShinyJsonContext] triggers our generator's
//            module-init auto-registration into Shiny.Json. ----------

[ShinyJsonContext]
[JsonSerializable(typeof(AutoType))]
[JsonSerializable(typeof(SingleOnlyType))]
[JsonSerializable(typeof(ForeignType))]
[JsonSerializable(typeof(BoxedInt))]
internal partial class AppJsonContext : JsonSerializerContext;


// A second, manually-registered context for DI tests.
[JsonSerializable(typeof(ManualType))]
[JsonSerializable(typeof(List<ManualType>))]
internal partial class ManualSerializationContext : JsonSerializerContext;


public class ManualType
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}


// A context/type registered by no default path — used to simulate a [ModuleInitializer]-based
// resolver install that fires after the shared options have already been built and frozen.
[JsonSerializable(typeof(LateType))]
internal partial class LateSerializationContext : JsonSerializerContext;


public class LateType
{
    public string Data { get; set; } = "";
}
