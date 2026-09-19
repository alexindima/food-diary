using FoodDiary.Modules.Statistics.Application.Mappings;
using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Statistics.Application.Common;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Statistics.Application.Queries.GetStatistics;

public sealed class GetStatisticsQueryHandler(
    ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetStatisticsQuery, Result<IReadOnlyList<AggregatedStatisticsModel>>> {
    public async Task<Result<IReadOnlyList<AggregatedStatisticsModel>>> Handle(
        GetStatisticsQuery request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<AggregatedStatisticsModel>>(userIdResult);
        }

        if (request.DateFrom > request.DateTo) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(
                Errors.Validation.Invalid(nameof(request.DateFrom), "DateFrom must be earlier than DateTo"));
        }

        if (!TemporalRangePolicy.IsPeriodWithinLimit(request.DateFrom, request.DateTo)) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(
                Errors.Validation.Invalid(
                    nameof(request.DateTo),
                    $"The period must not exceed {TemporalRangePolicy.MaxPeriodDays} days."));
        }

        if (!TemporalRangePolicy.IsQuantizationValid(request.QuantizationDays)) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(
                Errors.Validation.Invalid(
                    nameof(request.QuantizationDays),
                    $"Value must be between 1 and {TemporalRangePolicy.MaxQuantizationDays}."));
        }

        UserId userId = userIdResult.Value;
        DateTime normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo);

        Result<IReadOnlyList<MealNutritionStatisticsBucket>> statisticsResult = await sender.Send(new ReadMealNutritionStatisticsQuery(
            userId,
            normalizedFrom,
            normalizedTo,
            request.QuantizationDays),
            cancellationToken).ConfigureAwait(false);

        if (statisticsResult.IsFailure) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(statisticsResult.Error);
        }

        return Result.Success<IReadOnlyList<AggregatedStatisticsModel>>([.. statisticsResult.Value.Select(StatisticsMappings.ToModel)]);
    }

}
