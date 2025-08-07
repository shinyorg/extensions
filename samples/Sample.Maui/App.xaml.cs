namespace Sample.Maui;

public partial class App : Application
{
    readonly MainPage mainPage;
    
    public App(MainPage mainPage)
    {
        this.InitializeComponent();
        this.mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(this.mainPage);
}