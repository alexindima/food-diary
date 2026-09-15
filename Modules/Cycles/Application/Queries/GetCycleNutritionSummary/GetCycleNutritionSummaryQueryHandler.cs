using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Cycles.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Application.Abstractions.Models;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Application.Queries.GetCycleNutritionSummary;

public sealed class GetCycleNutritionSummaryQueryHandler(
    ICycleReadModelRepository cycleRepository,
    IMealNutritionStatisticsReadService statisticsReadService,
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

        if (query.DateFrom > query.DateTo) {
            return Result.Failure<CycleNutritionSummaryModel?>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be less than or equal to DateTo."));
        }

        if (query.DateTo.DayNumber - query.DateFrom.DayNumber > MaxSummaryRangeDays) {
            return Result.Failure<CycleNutritionSummaryModel?>(
                Errors.Validation.Invalid(nameof(query.DateTo), "Summary range must not exceed one year."));
        }

        CycleProfileReadModel? profile = await cycleRepository.GetCurrentReadModelAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Success<CycleNutritionSummaryModel?>(value: null);
        }
        if (!profile.HasActiveConsent(CycleConsentPurpose.NutritionInsights)) {
            return Result.Success<CycleNutritionSummaryModel?>(CycleNutritionSummaryCalculator.CreateConsentRequiredSummary(query.DateFrom, query.DateTo));
        }
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> nutrition = await statisticsReadService.GetStatisticsAsync(
            userIdResult.Value,
            query.DateFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            query.DateTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
            quantizationDays: 1,
            cancellationToken).ConfigureAwait(false);
        return nutrition.IsFailure
            ? Result.Failure<CycleNutritionSummaryModel?>(nutrition.Error)
            : Result.Success<CycleNutritionSummaryModel?>(CycleNutritionSummaryCalculator.BuildSummary(profile, nutrition.Value, query.DateFrom, query.DateTo));
    }
}
