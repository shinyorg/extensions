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

    [Service(ServiceLifetime.Transient, KeyedName = "ImplOnly")]
    public class KeyedImplementationOnly;


    [Service(ServiceLifetime.Singleton, TryAdd = true)]
    public class StandardImplementation : IStandardInterface;

    [Service(ServiceLifetime.Scoped, KeyedName = "Standard")]
    public class KeyedStandardImplementation : IStandardInterface;

    [Service(ServiceLifetime.Singleton)]
    public class MultipleImplementation : IStandardInterface, IStandardInterface2;

    [Service(ServiceLifetime.Scoped)]
    public class ScopedMultipleImplementation : IStandardInterface, IStandardInterface2;


    [Service(ServiceLifetime.Scoped, KeyedName = "KeyedGeneric", TryAdd = true)]
    public class TestGeneric<T1, T2>
    {
        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }
    }


    [Service(ServiceLifetime.Singleton, Category = "DEV1")]
    public class DevCategoryService;
    
    [Service(ServiceLifetime.Singleton, Category = "PROD")]
    public class ProdCategoryService;
}