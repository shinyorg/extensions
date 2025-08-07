namespace Shiny.Extensions.Device;

public interface IDeviceInfo
{
    string OperatingSystem { get; }
    string? OperatingSystemVersion { get; }
    string Manufacturer { get; }
    string? Model { get; }
}