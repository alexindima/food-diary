using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class DailyAdviceRepository(FoodDiaryDbContext context) : IDailyAdviceRepository {
    public async Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default) {
        string normalizedLocale = NormalizeLocale(locale);

        return await context.DailyAdvices
            .AsNoTracking()
            .Where(advice => advice.Locale == normalizedLocale)
            .OrderBy(advice => advice.Id)
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
