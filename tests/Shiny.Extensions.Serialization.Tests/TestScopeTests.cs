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


    [Fact(DisplayName = "Json - duplicate context install after options are frozen is ignored")]
    public void DuplicateResolver_AfterFreeze_Ignored()
    {
        try
        {
            // Build + use the serializer so its JsonSerializerOptions become read-only.
            Json.Default.Serialize(new AutoType { Title = "freeze" });

            // AppJsonContext is already registered via its module initializer. A second install
            // of the same context — as a late-firing [ModuleInitializer] or a DI extension would
            // do — must not throw even though the options are now frozen.
            Should.NotThrow(() => Json.AddContext(AppJsonContext.Default));

            // A distinct instance of the same context type is treated as the same registration.
            Should.NotThrow(() => Json.AddResolver(new AppJsonContext()));

            // The serializer still works after the ignored duplicate installs.
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
