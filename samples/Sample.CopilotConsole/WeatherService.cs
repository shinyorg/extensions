namespace Sample.CopilotConsole;

[Singleton]
public class WeatherService : IWeatherService
{
    static readonly string[] Conditions = ["Sunny", "Cloudy", "Rainy", "Snowy", "Windy", "Partly Cloudy"];

    public Task<WeatherResult> GetWeatherAsync(string city, string unit)
    {
        var random = new Random(city.GetHashCode());
        var tempC = random.Next(-10, 40);
        var temp = unit.Equals("fahrenheit", StringComparison.OrdinalIgnoreCase)
            ? tempC * 9.0 / 5.0 + 32
            : tempC;
        var condition = Conditions[random.Next(Conditions.Length)];

        return Task.FromResult(new WeatherResult(city, temp, unit, condition));
    }
}
