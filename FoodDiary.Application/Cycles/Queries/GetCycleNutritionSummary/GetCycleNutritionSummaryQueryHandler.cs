using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;

public sealed class GetCycleNutritionSummaryQueryHandler(
    ICycleReadService cycleReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetCycleNutritionSummaryQuery, Result<CycleNutritionSummaryModel?>> {
    private const int MaxSummaryRangeDays = 366;

    public async Task<Result<CycleNutritionSummaryModel?>> Handle(
        GetCycleNutritionSummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<CycleNutritionSummaryModel?>(userIdResult);
        }

        DateTime normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(query.DateTo);
        if (normalizedFrom > normalizedTo) {
            return Result.Failure<CycleNutritionSummaryModel?>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be less than or equal to DateTo."));
        }

        if ((normalizedTo - normalizedFrom).TotalDays > MaxSummaryRangeDays) {
            return Result.Failure<CycleNutritionSummaryModel?>(
                Errors.Validation.Invalid(nameof(query.DateTo), "Summary range must not exceed one year."));
        }

        return await cycleReadService.GetNutritionSummaryAsync(
            userIdResult.Value,
            normalizedFrom,
            normalizedTo,
            cancellationToken).ConfigureAwait(false);
    }
}
