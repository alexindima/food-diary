using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Modules.DailyAdvices.Application.Services;

internal static class DailyAdviceSelector {
    public static DailyAdviceReadModel? SelectReadModelForDate(
        IReadOnlyList<DailyAdviceReadModel> advices,
        DateTime date,
        string locale) {
        ArgumentNullException.ThrowIfNull(advices);

        if (advices.Count == 0) {
            return null;
        }

        string normalizedLocale = NormalizeLocale(locale);
        var filtered = advices
            .Where(a => string.Equals(a.Locale, normalizedLocale, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtered.Count == 0) {
            return null;
        }

        var ordered = filtered.OrderBy(a => a.Id).ToList();
        if (ordered.Count == 1) {
            return ordered[0];
        }

        DateTime targetDate = date.Date;
        long dayNumber = targetDate.Ticks / TimeSpan.TicksPerDay;
        if (ordered.Count == 2) {
            // With two choices, strict no-repeat selection necessarily alternates.
            int firstIndex = GetReadModelWeightedIndex(ordered, DateTime.MinValue, normalizedLocale);
            return ordered[(firstIndex + (int)(dayNumber % 2)) % 2];
        }

        // Even calendar days are independent weighted anchors. An intervening day
        // excludes both anchors, so neither boundary can repeat without walking history.
        if (dayNumber % 2 == 0) {
            return ordered[GetReadModelWeightedIndex(ordered, targetDate, normalizedLocale)];
        }

        int previousIndex = GetReadModelWeightedIndex(ordered, targetDate.AddDays(-1), normalizedLocale);
        int nextIndex = GetReadModelWeightedIndex(ordered, targetDate.AddDays(1), normalizedLocale);
        var candidates = ordered.Where((_, index) => index != previousIndex && index != nextIndex).ToList();
        return candidates[GetReadModelWeightedIndex(candidates, targetDate, normalizedLocale)];
    }

    private static int GetReadModelWeightedIndex(
        IReadOnlyList<DailyAdviceReadModel> advices,
        DateTime date,
        string locale) {
        long totalWeight = advices.Sum(advice => (long)Math.Max(1, advice.Weight));
        long hashValue = ComputeStableHash(string.Create(CultureInfo.InvariantCulture, $"{locale}:{date:yyyy-MM-dd}"));
        long offset = hashValue % totalWeight;

        long cumulative = 0;
        for (int i = 0; i < advices.Count - 1; i++) {
            cumulative += Math.Max(1, advices[i].Weight);
            if (offset < cumulative) {
                return i;
            }
        }

        return advices.Count - 1;
    }

    private static long ComputeStableHash(string input) {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA256.HashData(bytes);
        return BitConverter.ToInt64(hash, 0) & long.MaxValue;
    }

    internal static string NormalizeLocale(string locale) {
        return LanguageCode.FromPreferred(locale).Value;
    }
}
