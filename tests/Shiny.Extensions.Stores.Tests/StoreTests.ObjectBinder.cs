namespace Shiny.Extensions.Stores.Tests;


public partial class StoreTests
{
    
    [Theory(DisplayName = "Store Binding - Basic")]
    [MemberData(nameof(Data))]
    public void Binding_Basic(IKeyValueStore store)
    {
        var key = ObjectStoreBinder.GetBindingKey(typeof(TestBind), nameof(TestBind.StringProperty));
        var random = Guid.NewGuid().ToString();
        var values = this.SetupBinder<TestBind>(store);
        values.BoundObject.StringProperty = random;

        store.Contains(key).ShouldBeTrue();
        store.Get<string>(key).ShouldBe(random);
    }

    
    [Theory(DisplayName = "Store Binding - Persist")]
    [MemberData(nameof(Data))]
    public void Binding_Persist(IKeyValueStore store)
    {
        var values = this.SetupBinder<TestBind>(store);
        values.BoundObject.StringProperty = Guid.NewGuid().ToString();

        var obj2 = new TestBind();
        values.Binder.Bind(obj2, store);
        values.BoundObject.StringProperty.ShouldBe(obj2.StringProperty);
    }

    
    [Theory(DisplayName = "Store Binding - Nullifying Removes")]
    [MemberData(nameof(Data))]
    public void NullifyingRemoves(IKeyValueStore store)
    {
        var values = this.SetupBinder<TestBind>(store);
        var key = ObjectStoreBinder.GetBindingKey(typeof(TestBind), nameof(TestBind.StringProperty));

        values.BoundObject.StringProperty = Guid.NewGuid().ToString();
        store.Contains(key).ShouldBeTrue();

        values.BoundObject.StringProperty = null;
        store.Contains(key).ShouldBeFalse();
    }

    
    [Theory(DisplayName = "Store Binding - Default Value Removes")]
    [MemberData(nameof(Data))]
    public void DefaultValueRemoves(IKeyValueStore store)
    {
        var values = this.SetupBinder<TestBind>(store);
        var key = ObjectStoreBinder.GetBindingKey(typeof(TestBind), nameof(TestBind.IntValue));

        values.BoundObject.IntValue = 99;
        store.Get<int>(key).ShouldBe(99);

        values.BoundObject.IntValue = 0;
        store.Contains(key).ShouldBeFalse();
    }

    
    // [Theory(DisplayName = "Store Binding - Protected Setter")]
    // [MemberData(nameof(Data))]
    // public void Binding_ProtectedSetter(IKeyValueStore store)
    // {
    //     var values = this.SetupBinder<TestBind>(store);
    //     var key = ObjectStoreBinder.GetBindingKey(typeof(TestBind), nameof(TestBind.ProtectedSetterProperty));
    //
    //     values.BoundObject.SetProtectedProperty(Guid.NewGuid().ToString());
    //     store.Contains(key).ShouldBeFalse();
    // }


    // [Theory(DisplayName = "Store Binding - Protected Getter")]
    // [MemberData(nameof(Data))]
    // public void Binding_ProtectedGetter(IKeyValueStore store)
    // {
    //     var values = this.SetupBinder<TestBind>(store);
    //     var key = ObjectStoreBinder.GetBindingKey(typeof(TestBind), nameof(TestBind.ProtectedGetterProperty));
    //
    //     values.BoundObject.ProtectedGetterProperty = Guid.NewGuid().ToString();
    //     store.Contains(key).ShouldBeFalse();
    // }

    
    [Fact(DisplayName = "Store Binding - Attribute Binding")]
    public void AttributeBinding()
    {
        var allStores = Data.Select(x => x.First()).Cast<IKeyValueStore>().ToList();
        var factory = new KeyValueStoreFactory(allStores);
        var binder = new ObjectStoreBinder(factory);

        var obj = new AttributeTestBind();
        var random = Guid.NewGuid().ToString();
        binder.Bind(obj);
        obj.TestString = random;

        var key = ObjectStoreBinder.GetBindingKey(typeof(AttributeTestBind), nameof(AttributeTestBind.TestString));
        factory
            .GetStore("memory")
            .Get<string>(key)
            .ShouldBe(random);
    }


    
    (IObjectStoreBinder Binder, T BoundObject) SetupBinder<T>(IKeyValueStore store) where T : class, INotifyPropertyChanged, new()
    {
        this.currentStore = store;
        var factory = new KeyValueStoreFactory([store]);
        var binder = new ObjectStoreBinder(factory);

        var obj = new T();
        binder.Bind(obj, store);
        return (binder, obj);
    }
}
