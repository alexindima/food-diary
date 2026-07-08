using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;

public sealed class GetHydrationDailyTotalQueryHandler(
    IHydrationEntryReadService hydrationEntryReadService,
    IHydrationGoalService hydrationGoalService)
    : IQueryHandler<GetHydrationDailyTotalQuery, Result<HydrationDailyModel>> {
    public async Task<Result<HydrationDailyModel>> Handle(
        GetHydrationDailyTotalQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<HydrationDailyModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<double?> goalResult = await hydrationGoalService.GetCurrentGoalAsync(userId, cancellationToken).ConfigureAwait(false);
        if (goalResult.IsFailure) {
            return Result.Failure<HydrationDailyModel>(goalResult.Error);
        }

        DateTime dateUtc = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateUtc);
        int total = await hydrationEntryReadService.GetDailyTotalAsync(userId, dateUtc, cancellationToken).ConfigureAwait(false);

        var response = new HydrationDailyModel(dateUtc, total, goalResult.Value);
        return Result.Success(response);
    }
}
