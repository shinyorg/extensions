using Shiny;
using Shiny.Extensions.Serialization.Tests;

// Demonstrates the assembly-level form: pull a foreign type into the generated context.
[assembly: ShinyJsonInclude(typeof(ForeignType))]
