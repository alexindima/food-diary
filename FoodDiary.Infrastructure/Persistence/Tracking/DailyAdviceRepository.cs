using FoodDiary.Application.DailyAdvices.Common;
using FoodDiary.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class DailyAdviceRepository(FoodDiaryDbContext context) : IDailyAdviceRepository {
    public async Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default) {
        var normalizedLocale = NormalizeLocale(locale);

        return await context.DailyAdvices
            .AsNoTracking()
            .Where(advice => advice.Locale == normalizedLocale)
            .OrderBy(advice => advice.Id)
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeLocale(string locale) {
        if (string.IsNullOrWhiteSpace(locale)) {
            return "en";
        }

        var normalized = locale.Trim().ToLowerInvariant();
        var separatorIndex = normalized.IndexOfAny(new[] { '-', '_' });
        return separatorIndex > 0 ? normalized[..separatorIndex] : normalized;
    }
}
