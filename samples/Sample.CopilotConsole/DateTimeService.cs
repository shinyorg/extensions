namespace Sample.CopilotConsole;

[Singleton]
public class DateTimeService : IDateTimeService
{
    public Task<DateTimeResult> GetCurrentDateTimeAsync(string? timezone)
    {
        DateTimeOffset now;
        string tzName;

        if (string.IsNullOrWhiteSpace(timezone))
        {
            now = DateTimeOffset.UtcNow;
            tzName = "UTC";
        }
        else
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tzInfo);
            tzName = tzInfo.DisplayName;
        }

        return Task.FromResult(new DateTimeResult(now.ToString("yyyy-MM-dd HH:mm:ss zzz"), tzName));
    }
}
