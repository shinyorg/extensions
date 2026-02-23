using System.Runtime.CompilerServices;

namespace Shiny.Extensions.WebHosting.Tests;


public class VerifyInitializer
{
    [ModuleInitializer]
    public static void Init() =>
        VerifySourceGenerators.Initialize();
}
