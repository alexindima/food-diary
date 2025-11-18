using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Statistics;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Statistics.Queries.GetStatistics;

public class GetStatisticsQueryHandler(IMealRepository mealRepository)
    : IQueryHandler<GetStatisticsQuery, Result<IReadOnlyList<AggregatedStatisticsResponse>>>
{
    public async Task<Result<IReadOnlyList<AggregatedStatisticsResponse>>> Handle(
        GetStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId is null || request.UserId == UserId.Empty)
        {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsResponse>>(Errors.Authentication.InvalidToken);
        }

        if (request.DateFrom > request.DateTo)
        {
            return Result.Failure<IReadOnlyList<AggregatedStatisticsResponse>>(
                Errors.Validation.Invalid(nameof(request.DateFrom), "DateFrom must be earlier than DateTo"));
        }

        var quantizationDays = Math.Clamp(request.QuantizationDays <= 0 ? 1 : request.QuantizationDays, 1, 365);

        var normalizedFrom = DateTime.SpecifyKind(request.DateFrom, DateTimeKind.Utc).Date;
        var normalizedTo = DateTime.SpecifyKind(request.DateTo, DateTimeKind.Utc).Date.AddDays(1).AddTicks(-1);

        var meals = await mealRepository.GetByPeriodAsync(
            request.UserId.Value,
            normalizedFrom,
            normalizedTo,
            cancellationToken);

        var buckets = BuildBuckets(normalizedFrom, normalizedTo, quantizationDays);
        var responses = new List<AggregatedStatisticsResponse>(buckets.Count);

        foreach (var (bucketStart, bucketEnd) in buckets)
        {
            var bucketMeals = meals
                .Where(m => m.Date >= bucketStart && m.Date <= bucketEnd)
                .ToList();

            responses.Add(BuildResponse(bucketStart, bucketEnd, bucketMeals, quantizationDays));
        }

        return Result.Success<IReadOnlyList<AggregatedStatisticsResponse>>(responses);
    }

    private static AggregatedStatisticsResponse BuildResponse(
        DateTime bucketStart,
        DateTime bucketEnd,
        IReadOnlyCollection<Meal> meals,
        int quantizationDays)
    {
        if (meals.Count == 0)
        {
            return new AggregatedStatisticsResponse(bucketStart, bucketEnd, 0, 0, 0, 0, 0);
        }

        var totalCalories = meals.Sum(m => m.TotalCalories);
        var totalProteins = meals.Sum(m => m.TotalProteins);
        var totalFats = meals.Sum(m => m.TotalFats);
        var totalCarbs = meals.Sum(m => m.TotalCarbs);
        var totalFiber = meals.Sum(m => m.TotalFiber);

        var effectiveDays = Math.Max(1, (bucketEnd.Date - bucketStart.Date).Days + 1);

        return new AggregatedStatisticsResponse(
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
        int quantizationDays)
    {
        var buckets = new List<(DateTime, DateTime)>();
        var currentStart = from;

        while (currentStart <= to)
        {
            var currentEnd = currentStart.AddDays(quantizationDays).AddTicks(-1);
            if (currentEnd > to)
            {
                currentEnd = to;
            }

            buckets.Add((currentStart, currentEnd));
            currentStart = currentEnd.AddTicks(1);
        }

        return buckets;
    }
}
