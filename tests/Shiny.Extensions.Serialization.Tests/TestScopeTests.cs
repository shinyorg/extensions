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
