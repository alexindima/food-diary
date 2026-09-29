using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;

public sealed class DailyAdviceRepository(DbSet<DailyAdvice> advices) : IDailyAdviceReadModelRepository {
    public async Task<IReadOnlyList<DailyAdviceReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) =>
        await advices.AsNoTracking().OrderBy(advice => advice.Locale).ThenBy(advice => advice.Value).ThenBy(advice => advice.Id)
            .Select(advice => new DailyAdviceReadModel(advice.Id.Value, advice.Locale, advice.Value, advice.Tag, advice.Weight, advice.GroupId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DailyAdviceReadModel>> GetPageReadModelsAsync(
        int page,
        int limit,
        CancellationToken cancellationToken = default) =>
        await advices.AsNoTracking().OrderBy(advice => advice.Locale).ThenBy(advice => advice.Value).ThenBy(advice => advice.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(advice => new DailyAdviceReadModel(advice.Id.Value, advice.Locale, advice.Value, advice.Tag, advice.Weight, advice.GroupId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DailyAdviceReadModel>> GetGroupPageReadModelsAsync(
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        Guid[] groupIds = await advices.AsNoTracking()
            .Select(advice => advice.GroupId == Guid.Empty ? advice.Id.Value : advice.GroupId)
            .Distinct()
            .OrderBy(groupId => groupId)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        return await advices.AsNoTracking()
            .Where(advice => Enumerable.Contains(groupIds, advice.GroupId == Guid.Empty ? advice.Id.Value : advice.GroupId))
            .OrderBy(advice => advice.GroupId == Guid.Empty ? advice.Id.Value : advice.GroupId)
            .ThenBy(advice => advice.Locale)
            .Select(advice => new DailyAdviceReadModel(advice.Id.Value, advice.Locale, advice.Value, advice.Tag, advice.Weight, advice.GroupId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DailyAdviceReadModel>> GetByLocaleReadModelsAsync(
        string locale,
        CancellationToken cancellationToken = default) {
        string normalizedLocale = NormalizeLocale(locale);

        return await advices
            .AsNoTracking()
            .Where(advice => advice.Locale == normalizedLocale)
            .OrderBy(advice => advice.Id)
            .Select(advice => new DailyAdviceReadModel(
                advice.Id.Value,
                advice.Locale,
                advice.Value,
                advice.Tag,
                advice.Weight, advice.GroupId))
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
