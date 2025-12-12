using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class DailyAdviceRepository : IDailyAdviceRepository
{
    private readonly FoodDiaryDbContext _context;

    public DailyAdviceRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default)
    {
        var normalizedLocale = NormalizeLocale(locale);

        return await _context.DailyAdvices
            .AsNoTracking()
            .Where(advice => advice.Locale == normalizedLocale)
            .OrderBy(advice => advice.Id)
            .ToListAsync(cancellationToken);
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
