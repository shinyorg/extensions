using Microsoft.Maui.Devices;

namespace Shiny.Extensions.Device;


public class AppDeviceInfo : IDeviceInfo
{
    public string OperatingSystem => DeviceInfo.Platform.ToString();
    public string? OperatingSystemVersion => DeviceInfo.VersionString;
    public string Manufacturer => DeviceInfo.Manufacturer;
    public string? Model => DeviceInfo.Model;
}