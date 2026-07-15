namespace Shiny.Extensions.Serialization.Tests;


[Collection("ShinyJson")]
public class TestScopeTests
{
    [Fact(DisplayName = "Json - CreateTestScope adds extras and unwinds on dispose")]
    public void CreateTestScope_Unwinds()
    {
        // Outside the scope, ManualType has not been added — unknown.
        Should.Throw<InvalidOperationException>(
            () => Json.Default.Serialize(new ManualType { Name = "x" })
        );

        using (Json.CreateTestScope(extraResolvers: [ManualSerializationContext.Default]))
        {
            var json = Json.Default.Serialize(new ManualType { Name = "y" });
            Json.Default.Deserialize<ManualType>(json).Name.ShouldBe("y");
        }

        // Back outside, the extra is gone.
        Should.Throw<InvalidOperationException>(
            () => Json.Default.Serialize(new ManualType { Name = "z" })
        );
    }


    [Fact(DisplayName = "Json - resolver installed after options are frozen still resolves its types")]
    public void ResolverInstalledAfterFreeze_Works()
    {
        try
        {
            // Build + use the serializer so its JsonSerializerOptions become read-only. This
            // mirrors production: startup serialization (e.g. the geofence store) freezes the
            // shared options before AppSupport's storage services are ever constructed.
            Json.Default.Serialize(new AutoType { Title = "freeze" });

            // Register a brand-new context AFTER the freeze — exactly as a [ModuleInitializer]
            // does when its assembly is first touched at runtime (AppSupport's storage/cache
            // services, constructed lazily by DI). This must not throw on the frozen options.
            Should.NotThrow(() => Json.AddContext(LateSerializationContext.Default));

            // The already-built, frozen serializer now resolves the newly registered type via the
            // live resolver chain — no rebuild required.
            var json = Json.Default.Serialize(new LateType { Data = "y" });
            Json.Default.Deserialize<LateType>(json).Data.ShouldBe("y");
        }
        finally
        {
            Json.Reset();
        }
    }


    [Fact(DisplayName = "Json - duplicate context install is ignored")]
    public void DuplicateResolver_Ignored()
    {
        try
        {
            Json.Default.Serialize(new AutoType { Title = "freeze" });

            // AppJsonContext is already registered via its module initializer; re-installing the
            // same context (or another instance of the same type) is a no-op, not a duplicate entry.
            Should.NotThrow(() => Json.AddContext(AppJsonContext.Default));
            Should.NotThrow(() => Json.AddResolver(new AppJsonContext()));

            var json = Json.Default.Serialize(new AutoType { Title = "ok" });
            Json.Default.Deserialize<AutoType>(json).Title.ShouldBe("ok");
        }
        finally
        {
            Json.Reset();
        }
    }


    [Fact(DisplayName = "Json - Reset rebuilds serializer but keeps registered resolvers")]
    public void Reset_PreservesRegisteredResolvers()
    {
        var before = Json.Default;
        Json.Reset();
        var after = Json.Default;

        before.ShouldNotBeSameAs(after);

        // AutoType context is registered via module init — survives reset.
        after.Serialize(new AutoType { Title = "still works" })
            .ShouldNotBeNullOrEmpty();
    }
}
