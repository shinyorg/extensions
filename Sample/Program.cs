using Sample;

// Test the source generator
var services = new ServiceCollection();

// This should call the generated registration method
services.AddShinyServiceRegistry();
var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("Services registered successfully!");
Console.WriteLine($"Total services: {services.Count}");

// Test a few services
var implementationOnly = serviceProvider.GetService<ImplementationOnly>();
Console.WriteLine($"ImplementationOnly registered: {implementationOnly != null}");

var standardInterface = serviceProvider.GetService<IStandardInterface>();
Console.WriteLine($"IStandardInterface registered: {standardInterface != null}");

var keyedService = serviceProvider.GetKeyedService<KeyedImplementationOnly>("ImplOnly");
Console.WriteLine($"KeyedImplementationOnly registered: {keyedService != null}");

var recordService = serviceProvider.GetService<MyStandardSingletonRecord>();
Console.WriteLine($"MyStandardSingletonRecord registered: {recordService != null}");