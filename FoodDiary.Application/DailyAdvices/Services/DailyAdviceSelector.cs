using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Application.Abstractions.DailyAdvices.Models;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.DailyAdvices.Services;

internal static class DailyAdviceSelector {
    public static DailyAdvice? SelectForDate(
        IReadOnlyList<DailyAdvice> advices,
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

        var ordered = filtered.OrderBy(a => a.Id.Value).ToList();
        DateTime targetDate = date.Date;
        int todayIndex = GetWeightedIndex(ordered, targetDate, normalizedLocale);
        DailyAdvice selected = ordered[todayIndex];

        if (ordered.Count <= 1) {
            return selected;
        }

        int previousIndex = GetWeightedIndex(ordered, targetDate.AddDays(-1), normalizedLocale);
        if (ordered[previousIndex].Id == selected.Id) {
            selected = ordered[(todayIndex + 1) % ordered.Count];
        }

        return selected;
    }

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
        DateTime targetDate = date.Date;
        int todayIndex = GetReadModelWeightedIndex(ordered, targetDate, normalizedLocale);
        DailyAdviceReadModel selected = ordered[todayIndex];

        if (ordered.Count <= 1) {
            return selected;
        }

        int previousIndex = GetReadModelWeightedIndex(ordered, targetDate.AddDays(-1), normalizedLocale);
        if (ordered[previousIndex].Id == selected.Id) {
            selected = ordered[(todayIndex + 1) % ordered.Count];
        }

        return selected;
    }

    private static int GetWeightedIndex(
        IReadOnlyList<DailyAdvice> advices,
        DateTime date,
        string locale) {
        int totalWeight = advices.Sum(advice => Math.Max(1, advice.Weight));
        long hashValue = ComputeStableHash(string.Create(CultureInfo.InvariantCulture, $"{locale}:{date:yyyy-MM-dd}"));
        int offset = (int)(hashValue % totalWeight);

        int cumulative = 0;
        for (int i = 0; i < advices.Count - 1; i++) {
            cumulative += Math.Max(1, advices[i].Weight);
            if (offset < cumulative) {
                return i;
            }
        }

        return advices.Count - 1;
    }

    private static int GetReadModelWeightedIndex(
        IReadOnlyList<DailyAdviceReadModel> advices,
        DateTime date,
        string locale) {
        int totalWeight = advices.Sum(advice => Math.Max(1, advice.Weight));
        long hashValue = ComputeStableHash(string.Create(CultureInfo.InvariantCulture, $"{locale}:{date:yyyy-MM-dd}"));
        int offset = (int)(hashValue % totalWeight);

        int cumulative = 0;
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
