using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.DailyAdvices.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

public sealed class GetDailyAdviceQueryHandler(
    IDailyAdviceReadRepository adviceRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetDailyAdviceQuery, Result<DailyAdviceModel>> {
    public async Task<Result<DailyAdviceModel>> Handle(GetDailyAdviceQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<DailyAdviceModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DailyAdviceModel>(accessError);
        }

        string locale = DailyAdviceSelector.NormalizeLocale(query.Locale);
        IReadOnlyList<DailyAdvice> advices = await adviceRepository.GetByLocaleAsync(locale, cancellationToken).ConfigureAwait(false);

        if (advices.Count == 0 && !string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase)) {
            locale = "en";
            advices = await adviceRepository.GetByLocaleAsync(locale, cancellationToken).ConfigureAwait(false);
        }

        if (advices.Count == 0) {
            return Result.Failure<DailyAdviceModel>(Errors.DailyAdvice.NotFound(locale));
        }

        DailyAdvice? advice = DailyAdviceSelector.SelectForDate(advices, query.Date, locale);
        if (advice is null) {
            return Result.Failure<DailyAdviceModel>(Errors.DailyAdvice.NotFound(locale));
        }

        var response = new DailyAdviceModel(
            advice.Id.Value,
            advice.Locale,
            advice.Value,
            advice.Tag,
            advice.Weight);

        return Result.Success(response);
    }
}
