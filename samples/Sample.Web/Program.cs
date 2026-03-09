using Sample.Web;
using Shiny;

var builder = WebApplication.CreateBuilder(args);
builder.AddInfrastructureModules(new TestModule());

var app = builder.Build();
app.UseInfrastructureModules();

app.MapGet("/", () => "Hello World!");

app.Run();
