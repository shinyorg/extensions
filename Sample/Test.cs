using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.DependencyInjection;

// given the following code from a user
namespace Sample
{
    [Service(ServiceLifetime.Singleton)]
    public record MyStandardSingletonRecord : IStandardInterface;

    [Service(ServiceLifetime.Scoped)]
    public record MyStandardScopedRecord;
    
    
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


    [Service(ServiceLifetime.Scoped, "KeyedGeneric")]
    public class TestGeneric<T1, T2>
    {
        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }
    }
}