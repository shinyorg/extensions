using System.ComponentModel;

namespace Sample.CopilotConsole;

[Tool]
[Description("Provides current date and time information")]
public interface IDateTimeService
{
    [Description("Get the current date and time, optionally for a specific timezone")]
    Task<DateTimeResult> GetCurrentDateTimeAsync(
        [Description("IANA timezone name (e.g. 'America/New_York'). If omitted, returns UTC.")] string? timezone
    );
}

public record DateTimeResult(string DateTime, string Timezone);
