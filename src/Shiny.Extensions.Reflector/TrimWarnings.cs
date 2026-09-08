namespace Shiny.Extensions.Reflector;

/// <summary>
/// Shared justification messages for the reflection based APIs that cannot be statically analyzed
/// by the trimmer or the AOT compiler.
/// </summary>
static class TrimWarnings
{
    public const string TrueReflection =
        "True reflection enumerates properties on a type that is only known at runtime. Apply [Reflector] to the type so the source generator produces a trim safe reflector instead.";

    public const string JsonConverter =
        "ReflectorJsonConverter reflects over types and property types that are only known at runtime. Use a JsonSerializerContext for trimmed or AOT applications.";
}
