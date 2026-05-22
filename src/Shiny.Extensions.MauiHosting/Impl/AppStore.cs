using Microsoft.Extensions.Options;

namespace Shiny.Impl;

public sealed partial class AppStore : IAppStore
{
    // Mutable so platform partials can cache discovered IDs (e.g. iTunes trackId) back into options.
    readonly AppStoreOptions options;
    readonly HttpClient http;

    public AppStore(IOptions<AppStoreOptions>? options = null, HttpClient? httpClient = null)
    {
        this.options = options?.Value ?? new AppStoreOptions();
        this.http = httpClient ?? new HttpClient();
    }

    public Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default)
        => this.LookupCurrent(cancellationToken);

    public Task<bool> OpenStore() => this.OpenStoreCore();
    public Task<bool> OpenReviewPage() => this.OpenReviewPageCore();

    // Non-platform fallback. Platform partials in Platforms/{Apple,Android,Windows}/AppStore.cs
    // supply the real implementations on their respective TFMs.
#if !(IOS || MACCATALYST || ANDROID || WINDOWS)
    Task<AppStoreResult?> LookupCurrent(CancellationToken cancellationToken)
        => Task.FromResult<AppStoreResult?>(null);

    Task<bool> OpenStoreCore() => Task.FromResult(false);
    Task<bool> OpenReviewPageCore() => Task.FromResult(false);
#endif
}
