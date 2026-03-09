namespace Shiny;

public interface IAppForeground
{
    void OnForeground();
}

public interface IAppBackground
{
    void OnBackground();
}