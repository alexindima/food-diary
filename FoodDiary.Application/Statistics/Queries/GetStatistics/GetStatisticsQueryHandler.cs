using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Statistics.Queries.GetStatistics;

public class GetStatisticsQueryHandler(
    IMealRepository mealRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetStatisticsQuery, Result<IReadOnlyList<AggregatedStatisticsModel>>> {
    public async Task<Result<IReadOnlyList<AggregatedStatisticsModel>>> Handle(
        GetStatisticsQuery request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(request.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(userIdResult.Error);
        }

        if (request.DateFrom > request.DateTo) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(
                Errors.Validation.Invalid(nameof(request.DateFrom), "DateFrom must be earlier than DateTo"));
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(accessError);
        }

        int quantizationDays = Math.Clamp(request.QuantizationDays <= 0 ? 1 : request.QuantizationDays, 1, 365);

        DateTime normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo);

        IReadOnlyList<Meal> meals = await mealRepository.GetByPeriodAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            cancellationToken).ConfigureAwait(false);

        List<(DateTime Start, DateTime End)> buckets = BuildBuckets(normalizedFrom, normalizedTo, quantizationDays);
        var responses = new List<AggregatedStatisticsModel>(buckets.Count);

        foreach ((DateTime bucketStart, DateTime bucketEnd) in buckets) {
            var bucketMeals = meals
                .Where(m => m.Date >= bucketStart && m.Date <= bucketEnd)
                .ToList();

            responses.Add(BuildResponse(bucketStart, bucketEnd, bucketMeals));
        }

        return Result.Success<IReadOnlyList<AggregatedStatisticsModel>>(responses);
    }

    private static AggregatedStatisticsModel BuildResponse(
        DateTime bucketStart,
        DateTime bucketEnd,
        IReadOnlyCollection<Meal> meals) {
        if (meals.Count == 0) {
            return new AggregatedStatisticsModel(bucketStart, bucketEnd, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        double totalCalories = meals.Sum(m => m.TotalCalories);
        double totalProteins = meals.Sum(m => m.TotalProteins);
        double totalFats = meals.Sum(m => m.TotalFats);
        double totalCarbs = meals.Sum(m => m.TotalCarbs);
        double totalFiber = meals.Sum(m => m.TotalFiber);

        int effectiveDays = GetBucketDayCount(bucketStart, bucketEnd);

        return new AggregatedStatisticsModel(
            bucketStart,
            bucketEnd,
            Math.Round(totalCalories, 2),
            Math.Round(totalProteins / effectiveDays, 2),
            Math.Round(totalFats / effectiveDays, 2),
            Math.Round(totalCarbs / effectiveDays, 2),
            Math.Round(totalFiber / effectiveDays, 2),
            Math.Round(totalProteins, 2),
            Math.Round(totalFats, 2),
            Math.Round(totalCarbs, 2),
            Math.Round(totalFiber, 2));
    }

    private static List<(DateTime Start, DateTime End)> BuildBuckets(
        DateTime from,
        DateTime to,
        int quantizationDays) {
        var buckets = new List<(DateTime, DateTime)>();
        DateTime currentStart = from;

        while (currentStart <= to) {
            DateTime currentEnd = currentStart.AddDays(quantizationDays).AddTicks(-1);
            if (currentEnd > to) {
                currentEnd = to;
            }

            buckets.Add((currentStart, currentEnd));
            currentStart = currentEnd.AddTicks(1);
        }

        return buckets;
    }

    private static int GetBucketDayCount(DateTime bucketStart, DateTime bucketEnd) {
        double totalDays = (bucketEnd - bucketStart).TotalDays;

        return Math.Max(1, (int)Math.Ceiling(totalDays));
    }
}
