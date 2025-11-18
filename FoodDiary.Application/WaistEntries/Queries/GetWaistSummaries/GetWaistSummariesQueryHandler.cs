using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.WaistEntries;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;

public class GetWaistSummariesQueryHandler(IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetWaistSummariesQuery, Result<IReadOnlyList<WaistEntrySummaryResponse>>>
{
    public async Task<Result<IReadOnlyList<WaistEntrySummaryResponse>>> Handle(
        GetWaistSummariesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryResponse>>(Errors.User.NotFound());
        }

        if (query.QuantizationDays <= 0)
        {
            return Result.Failure<IReadOnlyList<WaistEntrySummaryResponse>>(
                Errors.Validation.Invalid(nameof(query.QuantizationDays), "Value must be greater than zero."));
        }

        var entries = await waistEntryRepository.GetByPeriodAsync(
            query.UserId.Value,
            query.DateFrom,
            query.DateTo,
            cancellationToken);

        var buckets = BuildBuckets(query.DateFrom, query.DateTo, query.QuantizationDays);
        var response = buckets
            .Select(bucket => BuildResponse(bucket.start, bucket.end, entries))
            .ToList();

        return Result.Success<IReadOnlyList<WaistEntrySummaryResponse>>(response);
    }

    private static IEnumerable<(DateTime start, DateTime end)> BuildBuckets(DateTime from, DateTime to, int step)
    {
        var current = from.Date;
        var end = to.Date;
        while (current <= end)
        {
            var bucketEnd = current.AddDays(step - 1);
            if (bucketEnd > end)
            {
                bucketEnd = end;
            }

            yield return (current, bucketEnd);
            current = bucketEnd.AddDays(1);
        }
    }

    private static WaistEntrySummaryResponse BuildResponse(
        DateTime start,
        DateTime end,
        IReadOnlyList<Domain.Entities.WaistEntry> entries)
    {
        var bucketEntries = entries
            .Where(entry => entry.Date >= start && entry.Date <= end)
            .ToList();

        if (bucketEntries.Count == 0)
        {
            return new WaistEntrySummaryResponse(start, end, 0);
        }

        var avg = bucketEntries.Average(entry => entry.Circumference);
        return new WaistEntrySummaryResponse(start, end, Math.Round(avg, 2));
    }
}
