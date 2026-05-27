namespace Sample
{
    [Singleton]
    public record MyStandardSingletonRecord : IStandardInterface;

    [Scoped]
    public record MyStandardScopedRecord;
    
    
    public interface IStandardInterface;

    public interface IStandardInterface2;

    [Singleton]
    public class ImplementationOnly;

    [Transient(KeyedName = "ImplOnly")]
    public class KeyedImplementationOnly;


    [Singleton(TryAdd = true)]
    public class StandardImplementation : IStandardInterface;

    [Scoped(KeyedName = "Standard")]
    public class KeyedStandardImplementation : IStandardInterface;

    [Singleton]
    public class MultipleImplementation : IStandardInterface, IStandardInterface2;

    [Scoped]
    public class ScopedMultipleImplementation : IStandardInterface, IStandardInterface2;


    [Scoped(KeyedName = "KeyedGeneric", TryAdd = true)]
    public class TestGeneric<T1, T2>
    {
        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }
    }


    [Singleton(Category = "DEV1")]
    public class DevCategoryService;
    
    [Singleton(Category = "PROD")]
    public class ProdCategoryService;


    [Singleton(AsSelf = true)]
    public class AsSelfTest : IStandardInterface;
    
    
    [Singleton(Type = typeof(IStandardInterface2))]
    public class SpecificTest : IStandardInterface, IStandardInterface2;
}