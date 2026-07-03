using System.Runtime.InteropServices;

namespace Shiny.Extensions.Stores;


/// <summary>
/// Detects whether the current process has package (MSIX) identity. WinRT
/// <c>Windows.Storage.ApplicationData.Current</c> — the backing for the native settings and secure
/// stores — is only available to packaged apps and throws otherwise, so unpackaged desktop apps
/// (WPF/WinForms/console targeting <c>net10.0-windows</c>) must use the file-backed fallback.
/// </summary>
static partial class WindowsPlatform
{
    // GetCurrentPackageFullName returns APPMODEL_ERROR_NO_PACKAGE (15700) when the process has no
    // package identity; any other result (ERROR_SUCCESS or ERROR_INSUFFICIENT_BUFFER) means packaged.
    const int APPMODEL_ERROR_NO_PACKAGE = 15700;


    static bool? isPackaged;
    public static bool IsPackaged => isPackaged ??= CheckPackaged();


    static bool CheckPackaged()
    {
        var length = 0;
        var rc = GetCurrentPackageFullName(ref length, IntPtr.Zero);
        return rc != APPMODEL_ERROR_NO_PACKAGE;
    }


    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentPackageFullName")]
    private static partial int GetCurrentPackageFullName(ref int packageFullNameLength, IntPtr packageFullName);
}
