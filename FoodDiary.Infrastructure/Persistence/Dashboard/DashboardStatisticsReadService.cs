using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardStatisticsReadService(FoodDiaryDbContext context) : IDashboardStatisticsReadService {
    public async Task<Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>> GetStatisticsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken = default) {
        if (dateFrom > dateTo) {
            return Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(
                Errors.Validation.Invalid(nameof(dateFrom), "DateFrom must be earlier than DateTo"));
        }

        int normalizedQuantizationDays = Math.Clamp(quantizationDays <= 0 ? 1 : quantizationDays, 1, 365);
        DateTime normalizedFrom = NormalizeUtcInstant(dateFrom);
        DateTime normalizedTo = NormalizeUtcInstant(dateTo);
        List<(DateTime Start, DateTime End)> buckets = BuildBuckets(normalizedFrom, normalizedTo, normalizedQuantizationDays);

        List<MealNutritionProjection> meals = await context.Meals
            .AsNoTracking()
            .Where(meal => meal.UserId == userId && meal.Date >= normalizedFrom && meal.Date <= normalizedTo)
            .Select(meal => new MealNutritionProjection(
                meal.Date,
                meal.TotalCalories,
                meal.TotalProteins,
                meal.TotalFats,
                meal.TotalCarbs,
                meal.TotalFiber))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>(
            [.. buckets.Select(bucket => BuildBucket(bucket.Start, bucket.End, meals))]);
    }

    private static DashboardStatisticsBucketReadModel BuildBucket(
        DateTime bucketStart,
        DateTime bucketEnd,
        IReadOnlyCollection<MealNutritionProjection> meals) {
        MealNutritionProjection[] bucketMeals = [.. meals.Where(meal => meal.Date >= bucketStart && meal.Date <= bucketEnd)];
        if (bucketMeals.Length == 0) {
            return new DashboardStatisticsBucketReadModel(bucketStart, bucketEnd, 0, 0, 0, 0, 0);
        }

        double totalCalories = bucketMeals.Sum(meal => meal.TotalCalories);
        double totalProteins = bucketMeals.Sum(meal => meal.TotalProteins);
        double totalFats = bucketMeals.Sum(meal => meal.TotalFats);
        double totalCarbs = bucketMeals.Sum(meal => meal.TotalCarbs);
        double totalFiber = bucketMeals.Sum(meal => meal.TotalFiber);
        int effectiveDays = GetBucketDayCount(bucketStart, bucketEnd);

        return new DashboardStatisticsBucketReadModel(
            bucketStart,
            bucketEnd,
            Math.Round(totalCalories, 2, MidpointRounding.ToEven),
            Math.Round(totalProteins / effectiveDays, 2, MidpointRounding.ToEven),
            Math.Round(totalFats / effectiveDays, 2, MidpointRounding.ToEven),
            Math.Round(totalCarbs / effectiveDays, 2, MidpointRounding.ToEven),
            Math.Round(totalFiber / effectiveDays, 2, MidpointRounding.ToEven),
            Math.Round(totalProteins, 2, MidpointRounding.ToEven),
            Math.Round(totalFats, 2, MidpointRounding.ToEven),
            Math.Round(totalCarbs, 2, MidpointRounding.ToEven),
            Math.Round(totalFiber, 2, MidpointRounding.ToEven));
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

    private static DateTime NormalizeUtcInstant(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    private sealed record MealNutritionProjection(
        DateTime Date,
        double TotalCalories,
        double TotalProteins,
        double TotalFats,
        double TotalCarbs,
        double TotalFiber);
}
