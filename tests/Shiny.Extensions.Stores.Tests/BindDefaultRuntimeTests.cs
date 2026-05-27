namespace Shiny.Extensions.Stores.Tests;


public enum LogLevel { Info, Warning, Error }


public partial class BindDefaultsHost
{
    [Bind(Default = "dark")]
    public partial string Theme { get; set; }

    [Bind(Default = 5)]
    public partial int RetryCount { get; set; }

    [Bind(Default = true)]
    public partial bool IsEnabled { get; set; }

    [Bind(Default = LogLevel.Warning)]
    public partial LogLevel Level { get; set; }

    [Bind(Default = 0)]
    public partial long Counter { get; set; }

    [Bind(Key = "auth-token", Default = "anonymous")]
    public partial string Token { get; set; }

    [Bind]
    public partial string? NoDefault { get; set; }
}


[Trait("Category", "BindDefaults")]
[Collection("ShinyStoresStatic")]
public class BindDefaultRuntimeTests : IDisposable
{
    public BindDefaultRuntimeTests()
    {
        Shiny.Stores.Reset();
        Shiny.Stores.Register(StoreKeys.Default, new MemoryKeyValueStore());
    }

    public void Dispose() => Shiny.Stores.Reset();


    [Fact(DisplayName = "Bind Default - string returns literal when store empty")]
    public void StringDefault_Empty()
    {
        var host = new BindDefaultsHost();
        host.Theme.ShouldBe("dark");
    }


    [Fact(DisplayName = "Bind Default - string returns assigned value after set")]
    public void StringDefault_AfterSet()
    {
        var host = new BindDefaultsHost();
        host.Theme = "light";
        host.Theme.ShouldBe("light");
    }


    [Fact(DisplayName = "Bind Default - string restored when underlying key removed")]
    public void StringDefault_RestoredAfterRemove()
    {
        var host = new BindDefaultsHost();
        host.Theme = "light";
        Shiny.Stores.Default.Remove("Theme").ShouldBeTrue();
        host.Theme.ShouldBe("dark");
    }


    [Fact(DisplayName = "Bind Default - int returns literal when store empty")]
    public void IntDefault_Empty()
    {
        var host = new BindDefaultsHost();
        host.RetryCount.ShouldBe(5);
    }


    [Fact(DisplayName = "Bind Default - int returns assigned value after set")]
    public void IntDefault_AfterSet()
    {
        var host = new BindDefaultsHost();
        host.RetryCount = 17;
        host.RetryCount.ShouldBe(17);
    }


    [Fact(DisplayName = "Bind Default - int zero is persisted, not treated as missing")]
    public void IntDefault_ZeroSetIsHonored()
    {
        var host = new BindDefaultsHost();
        host.RetryCount = 0;
        Shiny.Stores.Default.Contains("RetryCount").ShouldBeTrue();
        host.RetryCount.ShouldBe(0);
    }


    [Fact(DisplayName = "Bind Default - bool returns literal when store empty")]
    public void BoolDefault_Empty()
    {
        var host = new BindDefaultsHost();
        host.IsEnabled.ShouldBeTrue();
    }


    [Fact(DisplayName = "Bind Default - bool false overrides true default")]
    public void BoolDefault_AfterSet()
    {
        var host = new BindDefaultsHost();
        host.IsEnabled = false;
        host.IsEnabled.ShouldBeFalse();
    }


    [Fact(DisplayName = "Bind Default - enum returns literal when store empty")]
    public void EnumDefault_Empty()
    {
        var host = new BindDefaultsHost();
        host.Level.ShouldBe(LogLevel.Warning);
    }


    [Fact(DisplayName = "Bind Default - enum returns assigned value after set")]
    public void EnumDefault_AfterSet()
    {
        var host = new BindDefaultsHost();
        host.Level = LogLevel.Error;
        host.Level.ShouldBe(LogLevel.Error);
    }


    [Fact(DisplayName = "Bind Default - int literal widens to long property default")]
    public void LongDefault_FromIntLiteral()
    {
        var host = new BindDefaultsHost();
        host.Counter.ShouldBe(0L);
        host.Counter = long.MaxValue;
        host.Counter.ShouldBe(long.MaxValue);
    }


    [Fact(DisplayName = "Bind Default - custom Key override is used for storage")]
    public void Default_WithCustomKey()
    {
        var host = new BindDefaultsHost();
        host.Token.ShouldBe("anonymous");

        host.Token = "abc123";
        Shiny.Stores.Default.Contains("Token").ShouldBeFalse("Custom Key override must redirect storage");
        Shiny.Stores.Default.Contains("auth-token").ShouldBeTrue();
        Shiny.Stores.Default.Get<string>("auth-token").ShouldBe("abc123");
        host.Token.ShouldBe("abc123");
    }


    [Fact(DisplayName = "Bind Default - property without Default returns null when store empty")]
    public void NoDefault_ReturnsNull()
    {
        var host = new BindDefaultsHost();
        host.NoDefault.ShouldBeNull();
    }


    [Fact(DisplayName = "Bind Default - per-instance reads route through shared static store")]
    public void Defaults_SharedAcrossInstances()
    {
        var a = new BindDefaultsHost();
        var b = new BindDefaultsHost();

        a.Theme.ShouldBe("dark");
        b.Theme.ShouldBe("dark");

        a.Theme = "ocean";
        b.Theme.ShouldBe("ocean");
    }
}
