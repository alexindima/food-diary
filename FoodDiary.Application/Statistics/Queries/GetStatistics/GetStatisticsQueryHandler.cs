using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Statistics.Queries.GetStatistics;

public sealed class GetStatisticsQueryHandler(
    IDashboardStatisticsReadService statisticsReadService,
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

        UserId userId = userIdResult.Value;
        DateTime normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo);

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> statisticsResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            request.QuantizationDays,
            cancellationToken).ConfigureAwait(false);

        if (statisticsResult.IsFailure) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(statisticsResult.Error);
        }

        return Result.Success<IReadOnlyList<AggregatedStatisticsModel>>([.. statisticsResult.Value.Select(ToModel)]);
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
            model.TotalFiber);
}
