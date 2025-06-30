namespace Shiny.Extensions.Stores.Tests;


public partial class StoreTests
{
    [Trait("Category", "Extensions")]
    [Theory]
    [MemberData(nameof(Data))]
    public void SetT(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.Get<int>(nameof(this.SetT)).ShouldBe(0);

        this.currentStore.Set(nameof(this.SetT), 345);
        this.currentStore.Get<int>(nameof(this.SetT)).ShouldBe(345);
    }


    [Trait("Category", "Extensions")]
    [Theory]
    [MemberData(nameof(Data))]
    public void SetT_Nullable(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.Get<Guid>(nameof(this.SetT_Nullable)).ShouldBe(Guid.Empty);
        this.currentStore.Get<Guid?>(nameof(this.SetT_Nullable)).ShouldBeNull();

        var guid = Guid.NewGuid();
        this.currentStore.Set(nameof(this.SetT_Nullable), guid);
        this.currentStore.Get<Guid?>(nameof(this.SetT_Nullable)).ShouldBe(guid);
    }


    [Trait("Category", "Extensions")]
    [Theory]
    [MemberData(nameof(Data))]
    public void GuidTests(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.Get<Guid>(nameof(this.SetT_Nullable)).ShouldBe(Guid.Empty);
        this.currentStore.Get<Guid?>(nameof(this.SetT_Nullable)).ShouldBeNull();

        var guid = Guid.NewGuid();
        this.currentStore.Set(nameof(this.SetT_Nullable), guid);
        this.currentStore.Get<Guid?>(nameof(this.SetT_Nullable)).ShouldBe(guid);
    }


    [Trait("Category", "Extensions")]
    [Theory]
    [MemberData(nameof(Data))]
    public void Bools(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.Get<bool>(nameof(this.Bools)).ShouldBeFalse("Nothing set - should be return false");
        this.currentStore.Get<bool?>(nameof(this.Bools)).ShouldBeNull("Nothing set - nullable allowed - but not returning null");

        this.currentStore.Set(nameof(this.Bools), true);
        this.currentStore.Get<bool>(nameof(this.Bools)).ShouldBeTrue("Value should now be true");
    }


    [Trait("Category", "Extensions")]
    [Theory]
    [MemberData(nameof(Data))]
    public void SetDefault(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.SetDefault(nameof(this.SetDefault), "Initial Value").ShouldBeTrue("Default value could not be set");
        this.currentStore.SetDefault(nameof(this.SetDefault), "Second Value").ShouldBeFalse("Default value was set and should not have been");
        this.currentStore.Get<string>(nameof(this.SetDefault)).ShouldBe("Initial Value");
    }
}
