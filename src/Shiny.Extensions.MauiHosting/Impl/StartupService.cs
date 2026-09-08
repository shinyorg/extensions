using System.Reflection;
using Microsoft.Extensions.Options;
#if !(ANDROID || IOS || MACCATALYST)
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;
#endif

namespace Shiny.Impl;

public sealed partial class StartupService : IStartupService
{
    readonly StartupServiceOptions options;

    public StartupService(IOptions<StartupServiceOptions>? options = null)
        => this.options = options?.Value ?? new StartupServiceOptions();

    public Task<StartupServiceState> GetState(CancellationToken cancellationToken = default)
        => this.GetStateCore(cancellationToken);

    public Task<StartupServiceState> Register(CancellationToken cancellationToken = default)
        => this.RegisterCore(cancellationToken);

    public Task<StartupServiceState> Unregister(CancellationToken cancellationToken = default)
        => this.UnregisterCore(cancellationToken);

    public Task<bool> OpenSettings() => this.OpenSettingsCore();


    // MAUI's AppInfo can't supply the default here - it throws NotImplementedInReferenceAssembly on the
    // bare net10.0 TFM, which is exactly the target the desktop implementations run on.
    string GetIdentifier()
    {
        var identifier = this.options.Identifier;
        if (String.IsNullOrWhiteSpace(identifier))
            identifier = Assembly.GetEntryAssembly()?.GetName().Name;

        if (String.IsNullOrWhiteSpace(identifier))
            identifier = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? String.Empty);

        if (String.IsNullOrWhiteSpace(identifier))
            throw new InvalidOperationException("Could not determine a startup identifier - set StartupServiceOptions.Identifier");

        // The identifier becomes a file name on Linux and a registry value name on Windows, so refuse
        // anything that could escape those containers rather than silently writing somewhere else.
        if (identifier.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidOperationException($"StartupServiceOptions.Identifier '{identifier}' contains characters that aren't valid in a file name");

        return identifier;
    }


    /// <summary>
    /// Resolves what the OS should launch. Quoting is left to each platform because the escaping rules
    /// differ (freedesktop desktop entries vs. a Windows command line).
    /// </summary>
    (string Executable, IReadOnlyList<string> Arguments) ResolveCommand()
    {
        var args = new List<string>();
        var exe = this.options.ExecutablePath;

        if (String.IsNullOrWhiteSpace(exe))
        {
            exe = Environment.ProcessPath;
            if (String.IsNullOrWhiteSpace(exe))
                throw new InvalidOperationException("Could not resolve the current executable path - set StartupServiceOptions.ExecutablePath");

            // A framework-dependent app started as `dotnet app.dll` reports the shared host as its process
            // path, so the managed entry point has to be handed back as the first argument - otherwise the
            // OS would just launch the dotnet CLI at login.
            if (String.Equals(Path.GetFileNameWithoutExtension(exe), "dotnet", StringComparison.OrdinalIgnoreCase))
            {
                var entryPoint = Environment.GetCommandLineArgs().FirstOrDefault();
                if (!String.IsNullOrWhiteSpace(entryPoint))
                    args.Add(entryPoint);
            }
        }

        args.AddRange(this.options.Arguments);
        return (exe, args);
    }


#if ANDROID || IOS

    // Neither OS has a startup list an app can add itself to.
    public bool IsSupported => false;

    Task<StartupServiceState> GetStateCore(CancellationToken cancellationToken)
        => Task.FromResult(StartupServiceState.NotSupported);

    Task<StartupServiceState> RegisterCore(CancellationToken cancellationToken)
        => Task.FromResult(StartupServiceState.NotSupported);

    Task<StartupServiceState> UnregisterCore(CancellationToken cancellationToken)
        => Task.FromResult(StartupServiceState.NotSupported);

    Task<bool> OpenSettingsCore() => Task.FromResult(false);

#elif !MACCATALYST

    // Windows and Linux both land on the bare net10.0 build - MAUI's Windows head resolves the net10.0
    // asset today, and Linux has no MAUI platform TFM at all. macOS is handled by SMAppService over in
    // Platforms/Apple. Nothing here touches MAUI Essentials, which throws on this TFM.

    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string StartupApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    const int AppModelErrorNoPackage = 15700;

    static readonly char[] QuoteRequiredChars = [' ', '\t', '"'];
    static bool? isPackagedApp;

    /// <summary>
    /// MSIX-packaged apps are excluded: the OS virtualizes their HKCU writes into a per-package hive, so a
    /// Run entry written by one is invisible to the shell. Those apps need a windows.startupTask manifest
    /// declaration driven through WinRT, which this package can't reach from the bare net10.0 TFM.
    /// </summary>
    public bool IsSupported
        => IsLinuxDesktop || (OperatingSystem.IsWindows() && !IsPackagedApp());

    static bool IsLinuxDesktop => OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid();


    Task<StartupServiceState> GetStateCore(CancellationToken cancellationToken)
    {
        if (!this.IsSupported)
            return Task.FromResult(StartupServiceState.NotSupported);

        return Task.FromResult(
            OperatingSystem.IsWindows()
                ? this.GetWindowsState()
                : this.GetLinuxState()
        );
    }


    Task<StartupServiceState> RegisterCore(CancellationToken cancellationToken)
    {
        if (!this.IsSupported)
            return Task.FromResult(StartupServiceState.NotSupported);

        if (OperatingSystem.IsWindows())
            this.WriteWindowsEntry();
        else
            this.WriteLinuxEntry();

        // Read back rather than assuming Enabled - the user may have switched the entry off previously and
        // Windows keeps that decision in a separate key that an app isn't allowed to overwrite.
        return this.GetStateCore(cancellationToken);
    }


    Task<StartupServiceState> UnregisterCore(CancellationToken cancellationToken)
    {
        if (!this.IsSupported)
            return Task.FromResult(StartupServiceState.NotSupported);

        if (OperatingSystem.IsWindows())
            this.DeleteWindowsEntry();
        else
            this.DeleteLinuxEntry();

        return Task.FromResult(StartupServiceState.NotRegistered);
    }


    Task<bool> OpenSettingsCore()
    {
        // Linux has no desktop-environment-agnostic settings page for autostart entries.
        if (!OperatingSystem.IsWindows())
            return Task.FromResult(false);

        try
        {
            // UseShellExecute is what lets the ms-settings: protocol handler take over.
            using var process = Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true });
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }


    #region Windows

    [SupportedOSPlatform("windows")]
    StartupServiceState GetWindowsState()
    {
        var identifier = this.GetIdentifier();

        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        if (runKey?.GetValue(identifier) is not string value || String.IsNullOrWhiteSpace(value))
            return StartupServiceState.NotRegistered;

        // Task Manager and Settings switch an entry off by flipping a status byte here instead of removing
        // the Run value - the low bit of the first byte is the on/off flag (0x02/0x06 on, 0x03/0x09 off).
        using var approvedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedKeyPath, false);
        if (approvedKey?.GetValue(identifier) is byte[] { Length: > 0 } flags && (flags[0] & 0x01) == 0x01)
            return StartupServiceState.DisabledByUser;

        return StartupServiceState.Enabled;
    }


    [SupportedOSPlatform("windows")]
    void WriteWindowsEntry()
    {
        var (exe, args) = this.ResolveCommand();
        var commandLine = String.Join(" ", args.Prepend(exe).Select(CommandLineQuote));

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (key == null)
            throw new InvalidOperationException($@"Could not open HKCU\{RunKeyPath} for writing");

        key.SetValue(this.GetIdentifier(), commandLine, RegistryValueKind.String);
    }


    [SupportedOSPlatform("windows")]
    void DeleteWindowsEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(this.GetIdentifier(), false);
    }


    [SupportedOSPlatform("windows")]
    static bool IsPackagedApp()
    {
        // Asking for the package name with a zero-length buffer is the documented way to tell whether the
        // process has package identity at all - unpackaged processes answer APPMODEL_ERROR_NO_PACKAGE.
        if (isPackagedApp == null)
        {
            var length = 0;
            isPackagedApp = GetCurrentPackageFullName(ref length, IntPtr.Zero) != AppModelErrorNoPackage;
        }
        return isPackagedApp.Value;
    }


    [DllImport("kernel32.dll", ExactSpelling = true)]
    static extern int GetCurrentPackageFullName(ref int packageFullNameLength, IntPtr packageFullName);


    // CommandLineToArgvW rules: backslashes are only special immediately before a quote, and a run of
    // trailing backslashes would otherwise escape the closing quote.
    static string CommandLineQuote(string value)
    {
        if (value.Length > 0 && value.IndexOfAny(QuoteRequiredChars) < 0)
            return value;

        var sb = new StringBuilder(value.Length + 2).Append('"');
        for (var i = 0; i < value.Length; i++)
        {
            var backslashes = 0;
            while (i < value.Length && value[i] == '\\')
            {
                backslashes++;
                i++;
            }

            if (i == value.Length)
            {
                sb.Append('\\', backslashes * 2);
                break;
            }

            if (value[i] == '"')
                sb.Append('\\', (backslashes * 2) + 1).Append('"');
            else
                sb.Append('\\', backslashes).Append(value[i]);
        }
        return sb.Append('"').ToString();
    }

    #endregion

    #region Linux

    StartupServiceState GetLinuxState()
    {
        var path = this.GetAutoStartPath();
        if (!File.Exists(path))
            return StartupServiceState.NotRegistered;

        // Desktop environments switch an autostart entry off by flipping one of these keys rather than
        // deleting the file, so a file that exists isn't necessarily an entry that runs.
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Replace(" ", String.Empty).Trim();
            if (trimmed.Equals("Hidden=true", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("X-GNOME-Autostart-enabled=false", StringComparison.OrdinalIgnoreCase))
                return StartupServiceState.DisabledByUser;
        }
        return StartupServiceState.Enabled;
    }


    void WriteLinuxEntry()
    {
        var (exe, args) = this.ResolveCommand();
        var path = this.GetAutoStartPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var sb = new StringBuilder();
        sb.AppendLine("[Desktop Entry]");
        sb.AppendLine("Type=Application");
        sb.AppendLine("Version=1.0");
        sb.Append("Name=").AppendLine(DesktopEntryValue(this.options.DisplayName ?? this.GetIdentifier()));
        sb.Append("Exec=").AppendLine(String.Join(" ", args.Prepend(exe).Select(DesktopEntryQuote)));
        sb.AppendLine("Terminal=false");
        sb.AppendLine("NoDisplay=false");
        // Both keys are written so an entry the user previously switched off comes back enabled.
        sb.AppendLine("Hidden=false");
        sb.AppendLine("X-GNOME-Autostart-enabled=true");

        File.WriteAllText(path, sb.ToString());
    }


    void DeleteLinuxEntry()
    {
        var path = this.GetAutoStartPath();
        if (File.Exists(path))
            File.Delete(path);
    }


    string GetAutoStartPath()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (String.IsNullOrWhiteSpace(configHome))
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        return Path.Combine(configHome, "autostart", this.GetIdentifier() + ".desktop");
    }


    // Exec values are always quoted so paths with spaces work; inside quotes the desktop entry spec
    // requires backslash escaping of ", `, $ and \ itself.
    static string DesktopEntryQuote(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("`", "\\`")
            .Replace("$", "\\$");

        return "\"" + escaped + "\"";
    }


    // A newline would start a new key in the entry file, so it never gets written through.
    static string DesktopEntryValue(string value)
        => value.Replace("\r", String.Empty).Replace("\n", " ");

    #endregion
#endif
}
