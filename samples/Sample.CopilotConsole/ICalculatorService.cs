using System.ComponentModel;

namespace Sample.CopilotConsole;

[Tool]
[Description("Performs math calculations")]
public interface ICalculatorService
{
    [Description("Perform a math calculation with two numbers")]
    Task<CalculateResult> CalculateAsync(
        [Description("First operand")] double a,
        [Description("Second operand")] double b,
        [Description("The operation to perform: add, subtract, multiply, or divide")] string operation
    );
}

public record CalculateResult(double Result, string Expression);
