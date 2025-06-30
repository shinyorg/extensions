using System.Runtime.CompilerServices;

namespace Shiny.Extensions.DependencyInjection.Tests;


public class VerifyInitializer
{
    [ModuleInitializer]
    public static void Init() =>
        VerifySourceGenerators.Initialize();
}