namespace Shiny.Extensions.Device;

public interface ITimeZone
{
    TimeZoneInfo Current { get; }
    event EventHandler<TimeZoneInfo> Changed;
}