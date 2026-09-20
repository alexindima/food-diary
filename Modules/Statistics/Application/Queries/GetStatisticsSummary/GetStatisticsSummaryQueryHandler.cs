using FoodDiary.Modules.Statistics.Application.Mappings;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;
using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Statistics.Application.Common;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Statistics.Application.Queries.GetStatisticsSummary;

public sealed class GetStatisticsSummaryQueryHandler(
    ISender sender, ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetStatisticsSummaryQuery, Result<StatisticsSummaryModel>> {
    public async Task<Result<StatisticsSummaryModel>> Handle(
        GetStatisticsSummaryQuery request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<StatisticsSummaryModel>(userIdResult);
        }

        if (request.DateFrom > request.DateTo) {
            return Result.Failure<StatisticsSummaryModel>(
                Errors.Validation.Invalid(nameof(request.DateFrom), "DateFrom must be earlier than DateTo"));
        }

        if (!TemporalRangePolicy.IsPeriodWithinLimit(request.DateFrom, request.DateTo)) {
            return Result.Failure<StatisticsSummaryModel>(
                Errors.Validation.Invalid(
                    nameof(request.DateTo),
                    $"The period must not exceed {TemporalRangePolicy.MaxPeriodDays} days."));
        }

        if (!TemporalRangePolicy.IsQuantizationValid(request.QuantizationDays)) {
            return Result.Failure<StatisticsSummaryModel>(
                Errors.Validation.Invalid(
                    nameof(request.QuantizationDays),
                    $"Value must be between 1 and {TemporalRangePolicy.MaxQuantizationDays}."));
        }

        if (!BodyMetricDateRangePolicy.IsValid(request.BodyDateFrom, request.BodyDateTo)) {
            return Result.Failure<StatisticsSummaryModel>(Errors.Validation.Invalid(
                nameof(request.BodyDateFrom), "Provide both body dates in ascending order within the allowed period."));
        }

        UserId userId = userIdResult.Value;
        DateTime statisticsFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom);
        DateTime statisticsTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo);
        DateTime bodyFrom = request.BodyDateFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            ?? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.DateFrom);
        DateTime bodyTo = request.BodyDateTo?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            ?? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.DateTo);

        Result<IReadOnlyList<MealNutritionStatisticsBucket>> statisticsResult = await sender.Send(new ReadMealNutritionStatisticsQuery(
            userId,
            statisticsFrom,
            statisticsTo,
            request.QuantizationDays),
            cancellationToken).ConfigureAwait(false);
        if (statisticsResult.IsFailure) {
            return Result.Failure<StatisticsSummaryModel>(statisticsResult.Error);
        }

        IReadOnlyList<WeightEntrySummaryModel> weight = await sender.Send(new ReadWeightSummariesQuery(UserId: userId, DateFrom: bodyFrom, DateTo: bodyTo, QuantizationDays: request.QuantizationDays), cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WaistEntrySummaryModel> waist = await sender.Send(new ReadWaistSummariesQuery(UserId: userId, DateFrom: bodyFrom, DateTo: bodyTo, QuantizationDays: request.QuantizationDays), cancellationToken).ConfigureAwait(false);

        return Result.Success(new StatisticsSummaryModel(
            [.. statisticsResult.Value.Select(StatisticsMappings.ToModel)],
            weight,
            waist));
    }

}
