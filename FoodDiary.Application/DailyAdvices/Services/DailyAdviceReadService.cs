using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.Abstractions.DailyAdvices.Models;
using FoodDiary.Application.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices.Models;

namespace FoodDiary.Application.DailyAdvices.Services;

public sealed class DailyAdviceReadService(IDailyAdviceReadModelRepository adviceRepository)
    : IDailyAdviceReadService {
    public async Task<Result<DailyAdviceModel>> GetForDateAsync(
        DateTime date,
        string? locale,
        CancellationToken cancellationToken) {
        string normalizedLocale = DailyAdviceSelector.NormalizeLocale(locale ?? "en");
        IReadOnlyList<DailyAdviceReadModel> advices = await adviceRepository.GetByLocaleReadModelsAsync(normalizedLocale, cancellationToken).ConfigureAwait(false);

        if (advices.Count == 0 && !string.Equals(normalizedLocale, "en", StringComparison.OrdinalIgnoreCase)) {
            normalizedLocale = "en";
            advices = await adviceRepository.GetByLocaleReadModelsAsync(normalizedLocale, cancellationToken).ConfigureAwait(false);
        }

        if (advices.Count == 0) {
            return Result.Failure<DailyAdviceModel>(Errors.DailyAdvice.NotFound(normalizedLocale));
        }

        DailyAdviceReadModel? advice = DailyAdviceSelector.SelectReadModelForDate(advices, date, normalizedLocale);
        if (advice is null) {
            return Result.Failure<DailyAdviceModel>(Errors.DailyAdvice.NotFound(normalizedLocale));
        }

        return Result.Success(new DailyAdviceModel(
            advice.Id,
            advice.Locale,
            advice.Value,
            advice.Tag,
            advice.Weight));
    }
}
