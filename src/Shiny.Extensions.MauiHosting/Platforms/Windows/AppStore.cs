using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Windows.Services.Store;

namespace Shiny.Impl;

public sealed partial class AppStore
{
    async Task<AppStoreResult?> LookupCurrent(CancellationToken cancellationToken)
    {
        // Windows PackageName from AppInfo is the package family name — NOT the Store ProductId.
        // The DisplayCatalog API takes the ProductId, so it must be configured explicitly.
        if (string.IsNullOrWhiteSpace(this.options.WindowsProductId))
            return null;

        var url = $"https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds={Uri.EscapeDataString(this.options.WindowsProductId)}&market={Uri.EscapeDataString(this.options.CountryCode.ToUpperInvariant())}&languages=en-US";
        using var response = await this.http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("Products", out var products) || products.GetArrayLength() == 0)
            return null;

        var product = products[0];
        Version? storeVersion = null;
        DateTimeOffset? releasedAt = null;
        string? releaseNotes = null;

        if (product.TryGetProperty("DisplaySkuAvailabilities", out var skus) && skus.ValueKind == JsonValueKind.Array)
        {
            // A product can have multiple SKUs (free/paid/trial). Take the highest version across all
            // packages — that's the one the user would receive when updating.
            foreach (var sku in skus.EnumerateArray())
            {
                if (!sku.TryGetProperty("Sku", out var skuObj))
                    continue;

                if (skuObj.TryGetProperty("Properties", out var props)
                    && props.TryGetProperty("Packages", out var packages)
                    && packages.ValueKind == JsonValueKind.Array)
                {
                    foreach (var package in packages.EnumerateArray())
                    {
                        if (package.TryGetProperty("Version", out var verEl) && Version.TryParse(verEl.GetString(), out var v))
                        {
                            if (storeVersion == null || v > storeVersion)
                                storeVersion = v;
                        }
                    }
                }

                if (sku.TryGetProperty("LocalizedProperties", out var locals)
                    && locals.ValueKind == JsonValueKind.Array
                    && locals.GetArrayLength() > 0)
                {
                    var local = locals[0];
                    // DisplayCatalog doesn't expose release notes directly; ProductDescription is the closest equivalent.
                    if (releaseNotes == null && local.TryGetProperty("ProductDescription", out var pdesc))
                        releaseNotes = pdesc.GetString();
                }
            }
        }

        if (storeVersion == null && product.TryGetProperty("LastModifiedDate", out var mod) && mod.TryGetDateTimeOffset(out var dto))
            releasedAt = dto;

        if (storeVersion == null)
            return null;

        var current = AppInfo.Current.Version;
        return new AppStoreResult(
            storeVersion,
            current,
            storeVersion > current,
            $"ms-windows-store://pdp/?ProductId={this.options.WindowsProductId}",
            releaseNotes,
            releasedAt
        );
    }

    Task<bool> OpenStoreCore()
    {
        if (string.IsNullOrWhiteSpace(this.options.WindowsProductId))
            return Task.FromResult(false);

        return Launcher.Default.TryOpenAsync(new Uri($"ms-windows-store://pdp/?ProductId={this.options.WindowsProductId}"));
    }

    Task<bool> OpenReviewPageCore()
    {
        if (string.IsNullOrWhiteSpace(this.options.WindowsProductId))
            return Task.FromResult(false);

        return Launcher.Default.TryOpenAsync(new Uri($"ms-windows-store://review/?ProductId={this.options.WindowsProductId}"));
    }

    async Task<bool> RequestReviewCore()
    {
        try
        {
            var result = await StoreContext.GetDefault().RequestRateAndReviewAppAsync();
            return result.Status == StoreRateAndReviewStatus.Succeeded;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
