using Microsoft.Extensions.DependencyInjection;

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


    [Fact(DisplayName = "Store Binding - Attribute Binding")]
    public void AttributeBinding()
    {
        var memStore = new MemoryKeyValueStore();
        var serializer = CreateSerializer();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IKeyValueStore>("memory", memStore);
        var sp = services.BuildServiceProvider();

        var binder = new ObjectStoreBinder(sp, serializer);

        var obj = new AttributeTestBind();
        var random = Guid.NewGuid().ToString();
        binder.Bind(obj);
        obj.TestString = random;

        var key = ObjectStoreBinder.GetBindingKey(typeof(AttributeTestBind), nameof(AttributeTestBind.TestString));
        memStore.Get<string>(key).ShouldBe(random);
    }


    (IObjectStoreBinder Binder, T BoundObject) SetupBinder<T>(IKeyValueStore store) where T : class, INotifyPropertyChanged, new()
    {
        this.currentStore = store;
        var serializer = CreateSerializer();
        var services = new ServiceCollection().BuildServiceProvider();
        var binder = new ObjectStoreBinder(services, serializer);

        var obj = new T();
        binder.Bind(obj, store);
        return (binder, obj);
    }
}
