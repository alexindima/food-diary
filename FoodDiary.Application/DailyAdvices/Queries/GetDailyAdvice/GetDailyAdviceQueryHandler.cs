using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.DailyAdvices.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

public class GetDailyAdviceQueryHandler(
    IDailyAdviceRepository adviceRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetDailyAdviceQuery, Result<DailyAdviceModel>> {
    public async Task<Result<DailyAdviceModel>> Handle(GetDailyAdviceQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DailyAdviceModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null) {
            return Result.Failure<DailyAdviceModel>(Errors.User.NotFound(userId.Value));
        }

        var locale = DailyAdviceSelector.NormalizeLocale(query.Locale);
        var advices = await adviceRepository.GetByLocaleAsync(locale, cancellationToken);

        if (advices.Count == 0 && !string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase)) {
            locale = "en";
            advices = await adviceRepository.GetByLocaleAsync(locale, cancellationToken);
        }

        if (advices.Count == 0) {
            return Result.Failure<DailyAdviceModel>(Errors.DailyAdvice.NotFound(locale));
        }

        var advice = DailyAdviceSelector.SelectForDate(advices, query.Date, locale);
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
