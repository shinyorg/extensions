namespace Shiny.Extensions.Stores.Tests;


public partial class StoreTests
{
    [Theory(DisplayName = "Stores - Basic Set")]
    [MemberData(nameof(Data))]
    public void Set(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.Set("Test", "1");
        this.currentStore.Set("Test", "2");
        this.currentStore.Get(typeof(string), "Test").ShouldBe("2");
    }

    
    [Theory(DisplayName = "Stores - Contains")]
    [MemberData(nameof(Data))]
    public void ContainsTest(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.Contains(Guid.NewGuid().ToString()).ShouldBeFalse("Contains should have returned false");

        this.currentStore.Set("Test", "1");
        this.currentStore.Contains("Test").ShouldBeTrue("Contains should have returned true");
    }


    
    [Theory(DisplayName = "Stores - Remove")]
    [MemberData(nameof(Data))]
    public void RemoveTest(IKeyValueStore store)
    {
        this.currentStore = store;
        this.currentStore.Set("Test", "1");
        this.currentStore.Remove("Test").ShouldBeTrue("Remove should have returned success");
    }
}
