using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.DailyAdvices.Services;

public sealed class DailyAdviceReadService(IDailyAdviceReadRepository adviceRepository)
    : IDailyAdviceReadService {
    public async Task<Result<DailyAdviceModel>> GetForDateAsync(
        DateTime date,
        string? locale,
        CancellationToken cancellationToken) {
        string normalizedLocale = DailyAdviceSelector.NormalizeLocale(locale ?? "en");
        IReadOnlyList<DailyAdvice> advices = await adviceRepository.GetByLocaleAsync(normalizedLocale, cancellationToken).ConfigureAwait(false);

        if (advices.Count == 0 && !string.Equals(normalizedLocale, "en", StringComparison.OrdinalIgnoreCase)) {
            normalizedLocale = "en";
            advices = await adviceRepository.GetByLocaleAsync(normalizedLocale, cancellationToken).ConfigureAwait(false);
        }

        if (advices.Count == 0) {
            return Result.Failure<DailyAdviceModel>(Errors.DailyAdvice.NotFound(normalizedLocale));
        }

        DailyAdvice? advice = DailyAdviceSelector.SelectForDate(advices, date, normalizedLocale);
        if (advice is null) {
            return Result.Failure<DailyAdviceModel>(Errors.DailyAdvice.NotFound(normalizedLocale));
        }

        return Result.Success(new DailyAdviceModel(
            advice.Id.Value,
            advice.Locale,
            advice.Value,
            advice.Tag,
            advice.Weight));
    }
}
