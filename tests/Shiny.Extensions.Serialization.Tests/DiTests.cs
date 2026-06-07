namespace Shiny.Extensions.Serialization.Tests;


[Collection("ShinyJson")]
public class DiTests
{
    [Fact(DisplayName = "DI - AddJsonSerialization resolves the same instance as Json.Default")]
    public void AddJsonSerialization_SharedInstance()
    {
        var services = new ServiceCollection();
        services.AddJsonSerialization();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISerializer>().ShouldBeSameAs(Json.Default);
    }


    [Fact(DisplayName = "DI - AddJsonContext with a hand-written context round-trips")]
    public void AddJsonContext_ManualContext_RoundTrips()
    {
        using var scope = Json.CreateTestScope(extraResolvers: [ManualSerializationContext.Default]);

        var services = new ServiceCollection();
        services.AddJsonContext(ManualSerializationContext.Default);
        var serializer = services.BuildServiceProvider().GetRequiredService<ISerializer>();

        var json = serializer.Serialize(new ManualType { Name = "Allan", Age = 99 });
        var back = serializer.Deserialize<ManualType>(json);
        back.Name.ShouldBe("Allan");
        back.Age.ShouldBe(99);
    }


    [Fact(DisplayName = "DI - ConfigureJsonSerializer applies before first use")]
    public void Configure_AppliesBeforeFirstUse()
    {
        // Default is WriteIndented=false; configurator flips it on. This asserts both that the
        // configurator runs and that it observably changed the output shape.
        using var scope = Json.CreateTestScope(
            extraConfigure: o => o.WriteIndented = true
        );

        var json = Json.Default.Serialize(new AutoType { Title = "z" });
        json.ShouldContain("\n");
    }
}
