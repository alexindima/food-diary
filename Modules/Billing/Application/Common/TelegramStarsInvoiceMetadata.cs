using System.Text.Json;

namespace FoodDiary.Modules.Billing.Application.Common;

internal static class TelegramStarsInvoiceMetadata {
    internal const int ProductionPeriodSeconds = 2592000;

    internal static bool IsSupportedPeriod(int? seconds) => seconds is null or 300 or ProductionPeriodSeconds;

    internal static string Serialize(int? seconds, string invoiceUrl) => JsonSerializer.Serialize(new {
        subscriptionPeriodSeconds = seconds ?? ProductionPeriodSeconds,
        invoiceUrl,
    });

    internal static bool TryReadInvoiceUrl(string? metadata, out string url) {
        url = string.Empty;
        if (metadata is null) {
            return false;
        }
        try {
            using var document = JsonDocument.Parse(metadata);
            JsonElement root = document.RootElement;
            if (root.ValueKind is not JsonValueKind.Object || !root.TryGetProperty("invoiceUrl", out JsonElement value) ||
                value.ValueKind is not JsonValueKind.String || value.GetString() is not { Length: > 0 and <= 2048 } candidate ||
                !Uri.TryCreate(candidate, UriKind.Absolute, out Uri? invoice) || invoice.Scheme is not "https" ||
                invoice.Host is not "t.me" || invoice.Port != 443 || invoice.UserInfo.Length > 0 ||
                invoice.Query.Length > 0 || invoice.Fragment.Length > 0) {
                return false;
            }
            string path = invoice.AbsolutePath;
            ReadOnlySpan<char> slug = [];
            if (path.StartsWith("/$", StringComparison.Ordinal)) {
                slug = path.AsSpan(2);
            } else if (path.StartsWith("/invoice/", StringComparison.Ordinal)) {
                slug = path.AsSpan(9);
            }
            if (slug.IsEmpty || slug.IndexOfAnyExcept("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_=") >= 0) {
                return false;
            }
            url = invoice.AbsoluteUri;
            return true;
        } catch (JsonException) {
            return false;
        }
    }

    internal static bool TryReadPeriod(string? metadata, out int seconds) {
        // Orders created before period quoting were always thirty-day subscriptions.
        seconds = ProductionPeriodSeconds;
        if (metadata is null) {
            return true;
        }
        try {
            using var document = JsonDocument.Parse(metadata);
            JsonElement root = document.RootElement;
            return root.ValueKind is JsonValueKind.Object &&
                root.TryGetProperty("subscriptionPeriodSeconds", out JsonElement period) &&
                period.ValueKind is JsonValueKind.Number && period.TryGetInt32(out seconds) && IsSupportedPeriod(seconds);
        } catch (JsonException) {
            return false;
        }
    }
}
