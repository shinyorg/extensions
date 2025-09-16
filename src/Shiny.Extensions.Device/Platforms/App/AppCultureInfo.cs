using System.Globalization;

namespace Shiny.Extensions.Device;


public class AppCultureInfo : ICultureInfo
{
    public CultureInfo Current { get; }
    public event EventHandler<CultureInfo>? Changed;

    void Startup()
    {
#if IOS || MACCATALYST
        var code = Foundation.NSLocale.CurrentLocale.LanguageCode;
#elif ANDROID
        var code = Application.Context.Resources.Configuration.Locale.Variant;
        // Java.Util.Locale.Default.Language;
#else
        //CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        //CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
#endif
        // CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalDigits
    }
}