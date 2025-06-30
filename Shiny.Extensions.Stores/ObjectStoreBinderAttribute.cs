namespace Shiny.Extensions.Stores;


[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ObjectStoreBinderAttribute(string storeAlias) : Attribute
{
    public string StoreAlias => storeAlias;
}
