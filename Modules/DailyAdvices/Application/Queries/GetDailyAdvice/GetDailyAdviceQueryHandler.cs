using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvice;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Application.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.DailyAdvices.Application.Queries.GetDailyAdvice;

public sealed class GetDailyAdviceQueryHandler(
    IDailyAdviceReadModelRepository adviceRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetDailyAdviceQuery, Result<DailyAdviceModel>> {
    public async Task<Result<DailyAdviceModel>> Handle(GetDailyAdviceQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DailyAdviceModel>(userIdResult);
        }

        DateTime date = query.Date;
        string? locale = query.Locale;
        string normalizedLocale = DailyAdviceSelector.NormalizeLocale(locale ?? "en");
        IReadOnlyList<DailyAdviceReadModel> advices = await adviceRepository.GetByLocaleReadModelsAsync(normalizedLocale, cancellationToken).ConfigureAwait(false);

        if (advices.Count == 0 && !string.Equals(normalizedLocale, "en", StringComparison.OrdinalIgnoreCase)) {
            normalizedLocale = "en";
            advices = await adviceRepository.GetByLocaleReadModelsAsync(normalizedLocale, cancellationToken).ConfigureAwait(false);
        }

        if (advices.Count == 0) {
            return Result.Failure<DailyAdviceModel>(DailyAdviceErrors.NotFound(normalizedLocale));
        }

        DailyAdviceReadModel? advice = DailyAdviceSelector.SelectReadModelForDate(advices, date, normalizedLocale);
        if (advice is null) {
            return Result.Failure<DailyAdviceModel>(DailyAdviceErrors.NotFound(normalizedLocale));
        }

        return Result.Success(new DailyAdviceModel(
            advice.Id,
            advice.Locale,
            advice.Value,
            advice.Tag,
            advice.Weight));

    }
}
