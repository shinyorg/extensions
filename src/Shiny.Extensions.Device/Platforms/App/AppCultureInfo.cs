using System.Globalization;

namespace Shiny.Extensions.Device;


public class AppCultureInfo : ICultureInfo
{
    public CultureInfo Current { get; }
    public event EventHandler<CultureInfo>? Changed;
}