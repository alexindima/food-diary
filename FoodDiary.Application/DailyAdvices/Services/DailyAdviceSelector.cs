using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.DailyAdvices.Services;

internal static class DailyAdviceSelector
{
    public static DailyAdvice? SelectForDate(
        IReadOnlyList<DailyAdvice> advices,
        DateTime date,
        string locale)
    {
        if (advices.Count == 0)
        {
            return null;
        }

        var normalizedLocale = NormalizeLocale(locale);
        var filtered = advices
            .Where(a => string.Equals(a.Locale, normalizedLocale, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtered.Count == 0)
        {
            return null;
        }

        var ordered = filtered.OrderBy(a => a.Id.Value).ToList();
        var targetDate = date.Date;
        var todayIndex = GetWeightedIndex(ordered, targetDate, normalizedLocale);
        var selected = ordered[todayIndex];

        if (ordered.Count > 1)
        {
            var previousIndex = GetWeightedIndex(ordered, targetDate.AddDays(-1), normalizedLocale);
            if (ordered[previousIndex].Id == selected.Id)
            {
                selected = ordered[(todayIndex + 1) % ordered.Count];
            }
        }

        return selected;
    }

    private static int GetWeightedIndex(
        IReadOnlyList<DailyAdvice> advices,
        DateTime date,
        string locale)
    {
        var totalWeight = advices.Sum(advice => Math.Max(1, advice.Weight));
        var hashValue = ComputeStableHash($"{locale}:{date:yyyy-MM-dd}");
        var offset = (int)(hashValue % totalWeight);

        var cumulative = 0;
        for (var i = 0; i < advices.Count; i++)
        {
            cumulative += Math.Max(1, advices[i].Weight);
            if (offset < cumulative)
            {
                return i;
            }
        }

        return advices.Count - 1;
    }

    private static long ComputeStableHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToInt64(hash, 0) & long.MaxValue;
    }

    private static string NormalizeLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return "en";
        }

        var normalized = locale.Trim().ToLowerInvariant();
        var separatorIndex = normalized.IndexOfAny(new[] { '-', '_' });
        return separatorIndex > 0 ? normalized[..separatorIndex] : normalized;
    }
}
