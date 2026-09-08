namespace Shiny;

/// <summary>
/// Installs or removes the current application from the operating system's "launch at login/startup"
/// list on desktop platforms (Windows, macOS, Linux). Mobile platforms have no equivalent concept and
/// report <see cref="StartupServiceState.NotSupported"/> on every call.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// True when the running platform can manage startup registration - Windows (unpackaged), macOS 13+,
    /// and Linux. Check this before showing a "run at startup" toggle; everything else returns
    /// <see cref="StartupServiceState.NotSupported"/>.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Reads the current registration state back from the OS. The user (or an administrator) can turn a
    /// registered app off outside of your app, so never cache this across app sessions.
    /// </summary>
    Task<StartupServiceState> GetState(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs the app into the OS startup list and returns the state the OS settled on. The result can
    /// come back as <see cref="StartupServiceState.DisabledByUser"/>, <see cref="StartupServiceState.DisabledByPolicy"/>,
    /// or <see cref="StartupServiceState.RequiresApproval"/> when the user or policy has the final say -
    /// that is not a failure, it means the user has to finish the job in the OS settings
    /// (see <see cref="OpenSettings"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">The OS rejected the registration outright.</exception>
    Task<StartupServiceState> Register(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the app from the OS startup list. Succeeds silently when the app was never registered.
    /// </summary>
    Task<StartupServiceState> Unregister(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the OS screen where the user manages startup apps (macOS System Settings > General > Login Items,
    /// Windows Settings > Apps > Startup). Returns false where there is nothing to open (Linux, mobile).
    /// </summary>
    Task<bool> OpenSettings();
}


public enum StartupServiceState
{
    /// <summary>The platform has no startup list this library can manage.</summary>
    NotSupported,

    /// <summary>The app is not in the startup list.</summary>
    NotRegistered,

    /// <summary>The app is registered and will launch at login.</summary>
    Enabled,

    /// <summary>Registered, but the user turned it off in Task Manager / Settings / Login Items.</summary>
    DisabledByUser,

    /// <summary>Registered, but group policy or MDM blocks it. The app cannot override this.</summary>
    DisabledByPolicy,

    /// <summary>
    /// macOS only - the login item was submitted but the user has to approve it in System Settings
    /// before it will actually launch.
    /// </summary>
    RequiresApproval
}


public class StartupServiceOptions
{
    /// <summary>
    /// Identity of the startup entry. Defaults to the entry assembly name.
    /// <list type="bullet">
    /// <item>Windows: the value name written under <c>HKCU\...\CurrentVersion\Run</c>. MSIX-packaged apps
    /// aren't supported - the OS virtualizes that key for them.</item>
    /// <item>Linux: the file name of the <c>~/.config/autostart/{Identifier}.desktop</c> entry.</item>
    /// <item>macOS: ignored - <c>SMAppService</c> always registers the running app bundle.</item>
    /// </list>
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Friendly name written into the Linux desktop entry's <c>Name</c> key. Defaults to
    /// <see cref="Identifier"/>. Ignored on Windows and macOS, which take the name from the app itself.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Executable the OS should launch. Defaults to the current process path, expanding to
    /// <c>dotnet &lt;entry assembly&gt;</c> when the app runs framework-dependent through the shared host.
    /// Ignored on macOS, where the OS launches the registered app bundle itself.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Arguments appended to the startup command line - commonly a marker such as <c>--autostart</c> so the
    /// app can start minimized or in the tray. Honoured on Windows and Linux; macOS login items launch the
    /// app bundle itself and cannot carry arguments.
    /// </summary>
    public IList<string> Arguments { get; set; } = new List<string>();
}
