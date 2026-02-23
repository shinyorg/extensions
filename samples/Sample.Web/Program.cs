var builder = WebApplication.CreateBuilder(args);
builder.AddAllWebModules();

var app = builder.Build();
app.UseWebModules();

app.MapGet("/", () => "Hello World!");

app.Run();
