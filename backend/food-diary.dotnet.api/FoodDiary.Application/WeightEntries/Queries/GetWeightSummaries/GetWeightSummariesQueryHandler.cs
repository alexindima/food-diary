using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;

public class GetWeightSummariesQueryHandler(IWeightEntryRepository weightEntryRepository)
    : IQueryHandler<GetWeightSummariesQuery, Result<IReadOnlyList<WeightEntrySummaryResponse>>>
{
    public async Task<Result<IReadOnlyList<WeightEntrySummaryResponse>>> Handle(
        GetWeightSummariesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryResponse>>(Errors.User.NotFound());
        }

        if (query.DateFrom > query.DateTo)
        {
            return Result.Failure<IReadOnlyList<WeightEntrySummaryResponse>>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be earlier than DateTo"));
        }

        var quantizationDays = Math.Clamp(query.QuantizationDays <= 0 ? 1 : query.QuantizationDays, 1, 365);
        var normalizedFrom = DateTime.SpecifyKind(query.DateFrom, DateTimeKind.Utc).Date;
        var normalizedTo = DateTime.SpecifyKind(query.DateTo, DateTimeKind.Utc).Date;

        var entries = await weightEntryRepository.GetByPeriodAsync(
            query.UserId.Value,
            normalizedFrom,
            normalizedTo,
            cancellationToken);

        var buckets = BuildBuckets(normalizedFrom, normalizedTo, quantizationDays);
        var response = buckets
            .Select(bucket => BuildResponse(bucket.Start, bucket.End, entries))
            .ToList();

        return Result.Success<IReadOnlyList<WeightEntrySummaryResponse>>(response);
    }

    private static WeightEntrySummaryResponse BuildResponse(
        DateTime start,
        DateTime end,
        IReadOnlyList<Domain.Entities.WeightEntry> entries)
    {
        var bucketEntries = entries
            .Where(entry => entry.Date >= start && entry.Date <= end)
            .ToList();

        if (bucketEntries.Count == 0)
        {
            return new WeightEntrySummaryResponse(start, end, 0);
        }

        var avg = bucketEntries.Average(entry => entry.Weight);
        return new WeightEntrySummaryResponse(start, end, Math.Round(avg, 2));
    }

    private static List<(DateTime Start, DateTime End)> BuildBuckets(DateTime from, DateTime to, int quantizationDays)
    {
        var buckets = new List<(DateTime, DateTime)>();
        var currentStart = from;

        while (currentStart <= to)
        {
            var currentEnd = currentStart.AddDays(quantizationDays).AddTicks(-1);
            if (currentEnd.Date > to)
            {
                currentEnd = to;
            }

            buckets.Add((currentStart, currentEnd));
            currentStart = currentEnd.AddTicks(1);
        }

        return buckets;
    }
}
