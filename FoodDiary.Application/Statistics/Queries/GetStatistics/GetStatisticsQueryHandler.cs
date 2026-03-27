using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Statistics.Queries.GetStatistics;

public class GetStatisticsQueryHandler(IMealRepository mealRepository)
    : IQueryHandler<GetStatisticsQuery, Result<IReadOnlyList<AggregatedStatisticsModel>>> {
    public async Task<Result<IReadOnlyList<AggregatedStatisticsModel>>> Handle(
        GetStatisticsQuery request,
        CancellationToken cancellationToken) {
        if (request.UserId is null || request.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(Errors.Authentication.InvalidToken);
        }

        if (request.DateFrom > request.DateTo) {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsModel>>(
                Errors.Validation.Invalid(nameof(request.DateFrom), "DateFrom must be earlier than DateTo"));
        }

        var userId = new UserId(request.UserId.Value);

        var quantizationDays = Math.Clamp(request.QuantizationDays <= 0 ? 1 : request.QuantizationDays, 1, 365);

        var normalizedFrom = NormalizeToUtcDateStart(request.DateFrom);
        var normalizedTo = NormalizeToUtcDateEnd(request.DateTo);

        var meals = await mealRepository.GetByPeriodAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            cancellationToken);

        var buckets = BuildBuckets(normalizedFrom, normalizedTo, quantizationDays);
        var responses = new List<AggregatedStatisticsModel>(buckets.Count);

        foreach (var (bucketStart, bucketEnd) in buckets) {
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
            return new AggregatedStatisticsModel(bucketStart, bucketEnd, 0, 0, 0, 0, 0);
        }

        var totalCalories = meals.Sum(m => m.TotalCalories);
        var totalProteins = meals.Sum(m => m.TotalProteins);
        var totalFats = meals.Sum(m => m.TotalFats);
        var totalCarbs = meals.Sum(m => m.TotalCarbs);
        var totalFiber = meals.Sum(m => m.TotalFiber);

        var effectiveDays = Math.Max(1, (bucketEnd.Date - bucketStart.Date).Days + 1);

        return new AggregatedStatisticsModel(
            bucketStart,
            bucketEnd,
            Math.Round(totalCalories, 2),
            Math.Round(totalProteins / effectiveDays, 2),
            Math.Round(totalFats / effectiveDays, 2),
            Math.Round(totalCarbs / effectiveDays, 2),
            Math.Round(totalFiber / effectiveDays, 2));
    }

    private static List<(DateTime Start, DateTime End)> BuildBuckets(
        DateTime from,
        DateTime to,
        int quantizationDays) {
        var buckets = new List<(DateTime, DateTime)>();
        var currentStart = from;

        while (currentStart <= to) {
            var currentEnd = currentStart.AddDays(quantizationDays).AddTicks(-1);
            if (currentEnd > to) {
                currentEnd = to;
            }

            buckets.Add((currentStart, currentEnd));
            currentStart = currentEnd.AddTicks(1);
        }

        return buckets;
    }

    private static DateTime NormalizeToUtcDateStart(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    private static DateTime NormalizeToUtcDateEnd(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }
}
