using FoodDiary.Application.Abstractions.DailyAdvices.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Abstractions.DailyAdvices.Common;

public interface IDailyAdviceReadRepository {
    Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<DailyAdviceReadModel>> GetByLocaleReadModelsAsync(
        string locale,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<DailyAdvice> advices = await GetByLocaleAsync(locale, cancellationToken).ConfigureAwait(false);
        return [.. advices.Select(static advice => new DailyAdviceReadModel(
            advice.Id.Value,
            advice.Locale,
            advice.Value,
            advice.Tag,
            advice.Weight))];
    }
}
