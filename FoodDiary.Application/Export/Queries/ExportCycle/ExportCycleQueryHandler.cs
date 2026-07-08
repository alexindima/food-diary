using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Globalization;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Export.Queries.ExportCycle;

public sealed class ExportCycleQueryHandler(
    ICycleReadService cycleReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<ExportCycleQuery, Result<FileExportResult>> {
    private const int MaxExportRangeDays = 366;

    public async Task<Result<FileExportResult>> Handle(
        ExportCycleQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<FileExportResult>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DateTime normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(query.DateTo);
        if (normalizedFrom > normalizedTo) {
            return Result.Failure<FileExportResult>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be less than or equal to DateTo."));
        }

        if ((normalizedTo - normalizedFrom).TotalDays > MaxExportRangeDays) {
            return Result.Failure<FileExportResult>(
                Errors.Validation.Invalid(nameof(query.DateTo), "Export range must not exceed one year."));
        }

        CycleModel? cycle = await cycleReadService.GetCurrentAsync(userId, cancellationToken).ConfigureAwait(false);
        if (cycle is null) {
            return Result.Failure<FileExportResult>(Errors.Cycle.NotFound(Guid.Empty));
        }

        TimeSpan displayOffset = ResolveDisplayOffset(normalizedFrom, query.TimeZoneOffsetMinutes);
        string fromStr = normalizedFrom.Add(displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string toStr = normalizedTo.Add(displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return Result.Success(new FileExportResult(
            CycleCsvGenerator.Generate(cycle, normalizedFrom, normalizedTo),
            "text/csv",
            $"cycle-tracking-{fromStr}-to-{toStr}.csv"));
    }

    private static TimeSpan ResolveDisplayOffset(DateTime dateFrom, int? timeZoneOffsetMinutes) {
        if (timeZoneOffsetMinutes is >= -840 and <= 840) {
            return TimeSpan.FromMinutes(timeZoneOffsetMinutes.Value);
        }

        TimeSpan timeOfDay = dateFrom.TimeOfDay;
        return timeOfDay <= TimeSpan.FromHours(12)
            ? -timeOfDay
            : TimeSpan.FromDays(1) - timeOfDay;
    }
}
