using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection;

// given the following code from a user
namespace Sample
{
    public interface IStandardInterface;

    public interface IStandardInterface2;

    [Service(ServiceLifetime.Singleton)]
    public class ImplementationOnly;

    [Service(ServiceLifetime.Transient, "ImplOnly")]
    public class KeyedImplementationOnly;


    [Service(ServiceLifetime.Singleton)]
    public class StandardImplementation : IStandardInterface;

    [Service(ServiceLifetime.Scoped, "Standard")]
    public class KeyedStandardImplementation : IStandardInterface;

    [Service(ServiceLifetime.Singleton)]
    public class MultipleImplementation : IStandardInterface, IStandardInterface2;

    [Service(ServiceLifetime.Scoped)]
    public class ScopedMultipleImplementation : IStandardInterface, IStandardInterface2;
}