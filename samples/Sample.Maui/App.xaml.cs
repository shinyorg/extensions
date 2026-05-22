using Microsoft.Maui.Controls;

namespace Sample.Maui;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new NavigationPage(new AppSupportPage()));
}
