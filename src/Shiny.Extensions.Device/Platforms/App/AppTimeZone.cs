namespace Shiny.Extensions.Device;


public class AppTimeZone : ITimeZone
{
    public TimeZoneInfo Current { get; }


    EventHandler<TimeZoneInfo>? handler;
    public event EventHandler<TimeZoneInfo>? Changed
    {
        add
        {
            var start = handler == null;
            handler += value;
            if (start)
            {
                
            }
        }
        remove
        {
            handler -= value;
            if (handler == null)
            {
                // shutdown
            }
        }
    }
}