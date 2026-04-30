namespace Sample.CopilotConsole;

[Singleton]
public class CalculatorService : ICalculatorService
{
    public Task<CalculateResult> CalculateAsync(double a, double b, string operation)
    {
        var (result, expr) = operation.ToLowerInvariant() switch
        {
            "add" => (a + b, $"{a} + {b} = {a + b}"),
            "subtract" => (a - b, $"{a} - {b} = {a - b}"),
            "multiply" => (a * b, $"{a} * {b} = {a * b}"),
            "divide" when b != 0 => (a / b, $"{a} / {b} = {a / b}"),
            "divide" => (double.NaN, $"{a} / {b} = Error: Division by zero"),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };

        return Task.FromResult(new CalculateResult(result, expr));
    }
}
