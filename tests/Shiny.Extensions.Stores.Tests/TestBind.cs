namespace Shiny.Extensions.Stores.Tests;


[Reflector]
public partial class TestBind : ObservableObject
{
    [ObservableProperty]
    public partial string? StringProperty { get; set; }

    [ObservableProperty]
    public partial Guid? NullableProperty { get; set; }

    [ObservableProperty]
    public partial int IntValue { get; set; }

    [ObservableProperty]
    public partial string? ProtectedGetterProperty { get; set; }

    [ObservableProperty]
    public partial string? ProtectedSetterProperty { get; protected set; }

    public void SetProtectedProperty(string? value)
    {
        this.ProtectedSetterProperty = value;
    }
}
