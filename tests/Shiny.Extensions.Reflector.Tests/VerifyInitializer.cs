using System.Runtime.CompilerServices;

namespace Shiny.Extensions.Reflector.Tests;


public class VerifyInitializer
{
    [ModuleInitializer]
    public static void Init() =>
        VerifySourceGenerators.Initialize();
}