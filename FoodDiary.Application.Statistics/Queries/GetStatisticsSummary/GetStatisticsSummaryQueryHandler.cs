using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Statistics.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Statistics.Queries.GetStatisticsSummary;

public sealed class GetStatisticsSummaryQueryHandler(
    IDashboardStatisticsReadService statisticsReadService,
    IWeightEntryReadService weightEntryReadService,
    IWaistEntryReadService waistEntryReadService,
    ICurrentUserAccessService currentUserAccessService)
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

        if (request.QuantizationDays <= 0) {
            return Result.Failure<StatisticsSummaryModel>(
                Errors.Validation.Invalid(nameof(request.QuantizationDays), "Value must be greater than zero."));
        }

        UserId userId = userIdResult.Value;
        DateTime statisticsFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom);
        DateTime statisticsTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo);
        DateTime bodyFrom = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.DateFrom);
        DateTime bodyTo = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.DateTo);

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> statisticsResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            statisticsFrom,
            statisticsTo,
            request.QuantizationDays,
            cancellationToken).ConfigureAwait(false);
        if (statisticsResult.IsFailure) {
            return Result.Failure<StatisticsSummaryModel>(statisticsResult.Error);
        }

        IReadOnlyList<WeightEntrySummaryModel> weight = await weightEntryReadService.GetSummariesAsync(
            userId,
            bodyFrom,
            bodyTo,
            request.QuantizationDays,
            cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WaistEntrySummaryModel> waist = await waistEntryReadService.GetSummariesAsync(
            userId,
            bodyFrom,
            bodyTo,
            request.QuantizationDays,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new StatisticsSummaryModel(
            [.. statisticsResult.Value.Select(ToModel)],
            weight,
            waist));
    }

    private static AggregatedStatisticsModel ToModel(DashboardStatisticsBucketReadModel model) =>
        new(
            model.DateFrom,
            model.DateTo,
            model.TotalCalories,
            model.AverageProteins,
            model.AverageFats,
            model.AverageCarbs,
            model.AverageFiber,
            model.TotalProteins,
            model.TotalFats,
            model.TotalCarbs,
            model.TotalFiber,
            model.BreakfastCalories,
            model.LunchCalories,
            model.DinnerCalories,
            model.SnackCalories,
            model.MealCount,
            model.TrackedDayCount);
}
