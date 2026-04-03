using Shiny;

namespace Sample.Web;

public class TestModule : IWebModule
{
    public void Add(Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
    {
        Console.WriteLine("TestModule.Add");
    }

    public void Use(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        Console.WriteLine("TestModule.Use");
    }
}
