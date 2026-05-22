namespace Shiny;

public interface IAppStore
{
    Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default);
    Task<bool> OpenStore();
    Task<bool> OpenReviewPage();
}

public record AppStoreResult(
    Version StoreVersion,
    Version CurrentVersion,
    bool NeedsUpdate,
    string StoreUrl,
    string? ReleaseNotes = null,
    DateTimeOffset? ReleasedAt = null,
    double? AverageRating = null,
    long? RatingCount = null,
    string? MinimumOsVersion = null
);

public class AppStoreOptions
{
    /// <summary>
    /// iOS numeric App Store ID (e.g. "1234567890"). If null, resolved from the iTunes
    /// lookup by bundle identifier when calling <see cref="IAppStore.GetCurrent"/>.
    /// Required to deep-link to the store without first calling GetCurrent.
    /// </summary>
    public string? AppleAppId { get; set; }

    /// <summary>
    /// iOS bundle identifier. Auto-detected from the running bundle when null.
    /// </summary>
    public string? AppleBundleId { get; set; }

    /// <summary>
    /// Android package name. Auto-detected from the running app context when null.
    /// </summary>
    public string? AndroidPackageName { get; set; }

    /// <summary>
    /// Microsoft Store Product ID (e.g. "9NBLGGH4NNS1"). Required on Windows.
    /// </summary>
    public string? WindowsProductId { get; set; }

    /// <summary>
    /// Two-letter region code for iTunes lookups (default "us").
    /// </summary>
    public string CountryCode { get; set; } = "us";
}
