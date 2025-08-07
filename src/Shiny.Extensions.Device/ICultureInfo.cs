using System.Globalization;

namespace Shiny.Extensions.Device;


public interface ICultureInfo
{
    CultureInfo Current { get; }
    event EventHandler<CultureInfo> Changed;
}