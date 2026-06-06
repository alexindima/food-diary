using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Admin.Queries;

internal static class AdminBillingQueryFilters {
    public static AdminBillingListFilter Create(
        int page,
        int limit,
        string? provider,
        string? status,
        string? kind,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc) =>
        new(
            page <= 0 ? 1 : page,
            limit is > 0 and <= 100 ? limit : 20,
            NormalizeProvider(provider),
            NormalizeInvariant(status),
            NormalizeInvariant(kind),
            Normalize(search),
            NormalizeUtc(fromUtc),
            NormalizeUtc(toUtc));

    private static string? NormalizeProvider(string? value) {
        string? provider = Normalize(value);
        if (provider is null) {
            return null;
        }

        if (string.Equals(provider, BillingProviderNames.Paddle, StringComparison.OrdinalIgnoreCase)) {
            return BillingProviderNames.Paddle;
        }

        if (string.Equals(provider, BillingProviderNames.YooKassa, StringComparison.OrdinalIgnoreCase)) {
            return BillingProviderNames.YooKassa;
        }

        if (string.Equals(provider, BillingProviderNames.Stripe, StringComparison.OrdinalIgnoreCase)) {
            return BillingProviderNames.Stripe;
        }

        return provider;
    }

    private static string? NormalizeInvariant(string? value) =>
        Normalize(value)?.ToLowerInvariant();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value?.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value?.ToUniversalTime();
}
