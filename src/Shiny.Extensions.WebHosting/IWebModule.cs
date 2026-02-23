namespace Shiny;

public interface IWebModule
{
    void Add(Microsoft.AspNetCore.Builder.WebApplicationBuilder builder);
    void Use(Microsoft.AspNetCore.Builder.WebApplication app);
}
