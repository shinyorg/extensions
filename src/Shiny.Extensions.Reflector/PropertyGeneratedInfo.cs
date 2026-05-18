namespace Shiny.Extensions.Reflector;

public record PropertyGeneratedInfo(
    string Name, 
    Type Type, 
    bool HasSetter
);