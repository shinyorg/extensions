namespace Shiny.Extensions.Stores.Tests;


[ObjectStoreBinder("memory")]
[Reflector]
public partial class AttributeTestBind : ObservableObject
{
    [ObservableProperty]
    public partial string? TestString { get; set; }
}
