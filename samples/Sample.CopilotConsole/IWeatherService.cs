using System.ComponentModel;

namespace Sample.CopilotConsole;

[Tool]
[Description("Provides weather forecast information for cities")]
public interface IWeatherService
{
    [Description("Get the current weather forecast for a given city")]
    Task<WeatherResult> GetWeatherAsync(
        [Description("The city name to get weather for")] string city,
        [Description("Temperature unit: 'celsius' or 'fahrenheit'")] string unit
    );
}

public record WeatherResult(string City, double Temperature, string Unit, string Condition);
