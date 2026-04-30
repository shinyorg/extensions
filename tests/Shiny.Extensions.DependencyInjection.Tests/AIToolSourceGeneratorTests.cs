namespace Shiny.Extensions.DependencyInjection.Tests;

public class AIToolSourceGeneratorTests
{
    [Fact]
    public Task DoesNotGenerateWithoutAIReference()
    {
        var source = """
            using System.ComponentModel;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Does something")]
                    void DoSomething();
                }
            }
            """;

        // Uses VerifyDI (no AI reference) - should NOT generate AI tools
        return TestHelper.VerifyDI(source);
    }

    [Fact]
    public Task GeneratesForVoidMethod()
    {
        var source = """
            using System.ComponentModel;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Does something")]
                    void DoSomething();
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesForTaskMethod()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Does something async")]
                    Task DoSomethingAsync();
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesForTaskOfTMethod()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets a value")]
                    Task<string> GetValueAsync();
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesForSyncReturnMethod()
    {
        var source = """
            using System.ComponentModel;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets a value")]
                    string GetValue();
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithStringParameter()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets by name")]
                    Task<string> GetByNameAsync(
                        [Description("The name")] string name
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithIntParameter()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets by id")]
                    Task<string> GetByIdAsync(
                        [Description("The id")] int id
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithGuidParameter()
    {
        var source = """
            using System;
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets by id")]
                    Task<string> GetByIdAsync(
                        [Description("The unique id")] Guid id
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithBoolParameter()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Sets active state")]
                    Task SetActiveAsync(
                        [Description("Whether active")] bool isActive
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithDoubleParameter()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Sets the amount")]
                    Task SetAmountAsync(
                        [Description("The amount")] double amount
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithDecimalParameter()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Sets the price")]
                    Task SetPriceAsync(
                        [Description("The price")] decimal price
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithDateTimeParameter()
    {
        var source = """
            using System;
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets events since")]
                    Task<string> GetEventsSinceAsync(
                        [Description("The start date")] DateTime since
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithDateOnlyParameter()
    {
        var source = """
            using System;
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets events for date")]
                    Task<string> GetEventsForDateAsync(
                        [Description("The date")] DateOnly date
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithTimeOnlyParameter()
    {
        var source = """
            using System;
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Sets alarm")]
                    Task SetAlarmAsync(
                        [Description("The time")] TimeOnly time
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithEnumParameter()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                public enum Priority { Low, Medium, High }

                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Sets priority")]
                    Task SetPriorityAsync(
                        [Description("The priority level")] Priority priority
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithMultipleParameters()
    {
        var source = """
            using System;
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Creates an item")]
                    Task<string> CreateAsync(
                        [Description("The name")] string name,
                        [Description("The count")] int count,
                        [Description("Whether enabled")] bool enabled
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithCancellationToken()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets data")]
                    Task<string> GetDataAsync(
                        [Description("The query")] string query,
                        CancellationToken cancellationToken
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesMultipleMethods()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Does something")]
                    void DoSomething();

                    [Description("Does something async")]
                    Task DoSomethingAsync();

                    [Description("Gets something")]
                    Task<string> GetSomethingByNameAsync(
                        [Description("The name")] string name
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task SkipsMethodsWithoutDescription()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("This one gets a tool")]
                    Task<string> GetValueAsync();

                    // No [Description] - should NOT generate a tool
                    Task DoInternalAsync();
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task DoesNotGenerateWithoutToolAttribute()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Does something")]
                    Task DoSomethingAsync();
                }
            }
            """;

        // Interface has [Description] but no [Tool] - should not generate AI tools
        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesMultipleInterfaces()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("First service")]
                public interface IFirstService
                {
                    [Description("Gets first")]
                    Task<string> GetFirstAsync();
                }

                [Tool]
                [Description("Second service")]
                public interface ISecondService
                {
                    [Description("Gets second")]
                    Task<int> GetSecondAsync();
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithLongParameter()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets by big id")]
                    Task<string> GetByBigIdAsync(
                        [Description("The big id")] long id
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithDateTimeOffsetParameter()
    {
        var source = """
            using System;
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets events since")]
                    Task<string> GetEventsSinceAsync(
                        [Description("The start")] DateTimeOffset since
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithTimeSpanParameter()
    {
        var source = """
            using System;
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Sets timeout")]
                    Task SetTimeoutAsync(
                        [Description("The duration")] TimeSpan duration
                    );
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }

    [Fact]
    public Task GeneratesWithParameterWithoutDescription()
    {
        var source = """
            using System.ComponentModel;
            using System.Threading.Tasks;
            using Shiny;

            namespace TestNamespace
            {
                [Tool]
                [Description("My service")]
                public interface IMyService
                {
                    [Description("Gets by name")]
                    Task<string> GetByNameAsync(string name);
                }
            }
            """;

        return TestHelper.VerifyAITools(source);
    }
}
