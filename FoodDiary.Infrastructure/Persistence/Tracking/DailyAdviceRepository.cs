using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.Abstractions.DailyAdvices.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public sealed class DailyAdviceRepository(FoodDiaryDbContext context) : IDailyAdviceReadModelRepository {
    public async Task<IReadOnlyList<DailyAdviceReadModel>> GetByLocaleReadModelsAsync(
        string locale,
        CancellationToken cancellationToken = default) {
        string normalizedLocale = NormalizeLocale(locale);

        return await context.DailyAdvices
            .AsNoTracking()
            .Where(advice => advice.Locale == normalizedLocale)
            .OrderBy(advice => advice.Id)
            .Select(advice => new DailyAdviceReadModel(
                advice.Id.Value,
                advice.Locale,
                advice.Value,
                advice.Tag,
                advice.Weight))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeLocale(string locale) {
        if (string.IsNullOrWhiteSpace(locale)) {
            return "en";
        }

        string normalized = locale.Trim().ToLowerInvariant();
        int separatorIndex = normalized.IndexOfAny(['-', '_']);
        return separatorIndex > 0 ? normalized[..separatorIndex] : normalized;
    }
}
