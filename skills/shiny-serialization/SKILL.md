---
name: shiny-serialization
description: Generate and configure Shiny.Extensions.Serialization for .NET - centralized AOT-safe JSON serializer with source-generated context registration and collection wrappers
auto_invoke: true
triggers:
  - ISerializer
  - Shiny.Json
  - AddJsonSerialization
  - AddJsonContext
  - ConfigureJsonSerializer
  - ShinyJsonContext
  - ShinyJsonInclude
  - Shiny.Extensions.Serialization
---

# Shiny Serialization Skill

You are an expert in Shiny.Extensions.Serialization, a .NET library providing a centralized, AOT-safe JSON serializer with source-generated chaining of `JsonSerializerContext`s and collection wrappers.

## When to Use This Skill

Invoke this skill when the user wants to:
- Replace ad-hoc `JsonSerializer.Serialize/Deserialize` calls with a centralized `ISerializer`
- Make multiple library-owned `JsonSerializerContext`s compose without manual `services.AddJsonContext(...)` calls
- Add AOT-safe `List<T>`, `T[]`, or `IEnumerable<T>` support for a type whose element-level `JsonTypeInfo` is already source-generated (the "inline `[JsonConverter]` works, but `List<T>` throws" case)
- Provide a static serializer accessor that's usable before DI exists (e.g. mobile cold-start through `Shiny.Stores`)

## Library Overview

**Documentation**: https://shinylib.net/serialization/
**Repository**: https://github.com/shinyorg/extensions
**Package**: `Shiny.Extensions.Serialization`

`Shiny.Extensions.Serialization` ships a runtime + Roslyn source generator. The runtime owns one shared `JsonSerializerOptions` whose `TypeInfoResolverChain` collects every contributed `JsonSerializerContext` and custom resolver. The generator emits `[ModuleInitializer]`s that auto-register user contexts at load time, and AOT-safe collection wrappers for opted-in types.

## Core Pieces

| API | Purpose |
|---|---|
| `Shiny.Json.Default` | The shared `ISerializer`. Self-bootstrapping. Returned to DI by `AddJsonSerialization()`. |
| `Shiny.Json.AddContext(JsonSerializerContext)` | Register a context into the shared chain. Called by generated module inits. |
| `Shiny.Json.AddResolver(IJsonTypeInfoResolver)` | Register any custom resolver into the chain. |
| `Shiny.Json.Configure(Action<JsonSerializerOptions>)` | Mutate options before the serializer is first used. |
| `Shiny.Json.Reset()` / `Shiny.Json.CreateTestScope(...)` | Test isolation — rebuild from the registered set on next access. |
| `services.AddJsonSerialization()` | DI: register `ISerializer` resolving to `Shiny.Json.Default`. |
| `services.AddJsonContext(context)` | DI shortcut: `Json.AddContext` + `AddJsonSerialization`. |
| `services.ConfigureJsonSerializer(cfg)` | DI shortcut: `Json.Configure` + `AddJsonSerialization`. |
| `[Shiny.ShinyJsonContext]` | On a user-declared `JsonSerializerContext` partial → generator emits a `[ModuleInitializer]` calling `Shiny.Json.AddContext`. |
| `[Shiny.ShinyJsonInclude]` (type-level) | Opt a type into AOT-safe collection wrappers (`List<T>`, `T[]`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IList<T>`, `ICollection<T>`, `IAsyncEnumerable<T>`). |
| `[assembly: Shiny.ShinyJsonInclude(typeof(T))]` | Same opt-in but for foreign types you don't own. |

## Setup

```csharp
// DI (optional — Shiny.Json.Default works without it)
services.AddJsonSerialization();
```

That's it for DI. The generator does the rest by reading `[ShinyJsonContext]`/`[ShinyJsonInclude]` markers in your source.

### Auto-registering a hand-written context

The recommended path. Decorate any normal STJ source-generator context with `[Shiny.ShinyJsonContext]`:

```csharp
using System.Text.Json.Serialization;
using Shiny;

[ShinyJsonContext]
[JsonSerializable(typeof(MyDto))]
[JsonSerializable(typeof(MyOtherDto))]
internal partial class MyAppJsonContext : JsonSerializerContext;
```

The Shiny generator emits (in a hidden file):

```csharp
[ModuleInitializer]
internal static void Init() => global::Shiny.Json.AddContext(MyAppJsonContext.Default);
```

The context registers before `Main` runs, so the static `Shiny.Json.Default` and any DI-resolved `ISerializer` both see the types. **You do not need `services.AddJsonContext(MyAppJsonContext.Default)` anywhere.**

### Adding collection support

If an element type has `JsonTypeInfo` (from some context — auto-registered, hand-written + `AddJsonContext`, or anything else in the chain) but `List<T>`/`T[]` throw "no metadata" under AOT, mark the element type:

```csharp
[ShinyJsonInclude]
public partial class MyDto
{
    public string Name { get; set; } = "";
}
```

Or, for a foreign type you don't own:

```csharp
[assembly: Shiny.ShinyJsonInclude(typeof(SomeExternal.Vendor.Payload))]
```

The generator emits an `IJsonTypeInfoResolver` providing `JsonTypeInfo<List<MyDto>>`, `JsonTypeInfo<MyDto[]>`, `JsonTypeInfo<IEnumerable<MyDto>>`, `JsonTypeInfo<IReadOnlyList<MyDto>>`, `JsonTypeInfo<IList<MyDto>>`, `JsonTypeInfo<ICollection<MyDto>>`, and `JsonTypeInfo<IAsyncEnumerable<MyDto>>`. Each one lazy-resolves the element `JsonTypeInfo<MyDto>` from the chain at runtime — composes with any context that supplies it.

## Composing with Inline `JsonConverter<T>`

This is the original mediator pain point. A type carrying `[JsonConverter(typeof(MyConverter))]` serializes fine in isolation, but `List<MyType>` throws under AOT because STJ has no `JsonTypeInfo<List<MyType>>`. Fix:

```csharp
[ShinyJsonInclude]
[JsonConverter(typeof(BoxedIntConverter))]
public partial class BoxedInt { public int Value { get; set; } }
```

Now `BoxedInt` is serialized through the inline converter (bare number, not an object) AND `List<BoxedInt>`, `BoxedInt[]`, etc. go through the generator-emitted collection wrappers. The wrappers lazy-fetch the element `JsonTypeInfo` which carries the inline converter — composition works.

## DI Patterns

```csharp
// Inject ISerializer anywhere
public class MyService(ISerializer serializer)
{
    public string Save(MyDto d) => serializer.Serialize(d);
    public MyDto Load(string j) => serializer.Deserialize<MyDto>(j);
}

// Mutate options before first use
services.ConfigureJsonSerializer(opts => opts.WriteIndented = false);

// Hand-add a context (for cases where you can't or won't decorate it with [ShinyJsonContext])
services.AddJsonContext(ThirdPartyJsonContext.Default);
```

## Static Patterns

```csharp
// Anywhere — no DI needed
var json = Shiny.Json.Default.Serialize(new MyDto { Name = "x" });
var back = Shiny.Json.Default.Deserialize<MyDto>(json);

// Late additions (before first Serialize call)
Shiny.Json.Configure(o => o.WriteIndented = false);
Shiny.Json.AddContext(SomeOtherContext.Default);
```

## Diagnostics

| ID | Severity | Meaning |
|---|---|---|
| `SJSON002` | Error | `[ShinyJsonInclude]` applied to an unbound generic — use a closed constructed type. |
| `SJSON003` | Warning | `[ShinyJsonInclude]` was applied to type `T`, but no `[JsonSerializable(typeof(T))]` is declared on any `JsonSerializerContext` in this compilation. Generated collection wrappers will return `null` at runtime and serialization will throw — add the `[JsonSerializable]` to a registered context (or accept the warning if the element comes from a different assembly). |

## Replacing the Serializer Wholesale

For non-JSON payloads (MessagePack, MemoryPack, etc.), swap the whole `ISerializer`:

```csharp
// Simple — pre-built instance
services.AddSerializer(new MyMessagePackSerializer());

// DI-constructed
services.AddSerializer<MyConfiguredSerializer>();
host.Services.UseSerializer();   // call after Build() to snapshot into Shiny.Json.Default
```

`UseSerializer()` mirrors `UseShinyStores()` — needed when the custom `ISerializer` takes DI dependencies.

## Tests

```csharp
[Collection("ShinyJson")]
public class MyTests
{
    [Fact]
    public void Foo()
    {
        using var scope = Shiny.Json.CreateTestScope(
            extraResolvers: [ExtraContext.Default],
            extraConfigure: o => o.WriteIndented = false
        );
        // Scope disposes → registry trims back, cached serializer reset.
    }
}
```

Tests touching `Shiny.Json` must share an xUnit collection (`[Collection("ShinyJson")]`) because the registry is process-static.

## Best Practices

1. **`[ShinyJsonContext]` over manual `AddJsonContext`** — module-init registration avoids "I forgot to call `services.AddJsonContext(...)` in this code path" bugs (which is exactly the latent bug we just patched in Shiny Locations GPS).
2. **`[ShinyJsonInclude]` per element type, not per collection** — never use `[assembly: ShinyJsonInclude(typeof(List<Foo>))]`. The generator already emits all the standard collection shapes from the element type.
3. **Pair `[ShinyJsonInclude]` with a `[JsonSerializable(typeof(T))]` in the same assembly** to silence `SJSON003`. If the element is registered in another assembly, suppress or accept the warning.
4. **Don't put `[ShinyJsonContext]` on a context owned by another package** — decorate types in your own code, not someone else's. For 3rd-party contexts you don't own, fall back to `services.AddJsonContext(ThirdPartyContext.Default)`.
5. **`Shiny.Json.Configure` runs before first use only.** Once `Default` has been touched, `JsonSerializerOptions` freezes. `Configure` calls after that mutate the same options instance and may throw on certain properties — call early or wrap in `CreateTestScope`.
