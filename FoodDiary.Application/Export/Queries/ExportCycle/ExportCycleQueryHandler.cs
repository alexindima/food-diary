using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Export.Queries.ExportCycle;

public class ExportCycleQueryHandler(
    ICycleReadRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<ExportCycleQuery, Result<FileExportResult>> {
    private const int MaxExportRangeDays = 366;

    public async Task<Result<FileExportResult>> Handle(
        ExportCycleQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FileExportResult>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FileExportResult>(accessError);
        }

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

        CycleProfile? profile = await cycleRepository.GetCurrentAsync(
            userId,
            includeDetails: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Failure<FileExportResult>(Errors.Cycle.NotFound(Guid.Empty));
        }

        TimeSpan displayOffset = ResolveDisplayOffset(normalizedFrom, query.TimeZoneOffsetMinutes);
        string fromStr = normalizedFrom.Add(displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string toStr = normalizedTo.Add(displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return Result.Success(new FileExportResult(
            CycleCsvGenerator.Generate(profile, normalizedFrom, normalizedTo),
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
