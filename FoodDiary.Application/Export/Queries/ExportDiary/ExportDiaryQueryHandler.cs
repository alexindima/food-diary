using System.Globalization;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Export.Queries.ExportDiary;

public sealed class ExportDiaryQueryHandler(
    IMealReadRepository mealRepository,
    ICurrentUserAccessService currentUserAccessService,
    IDiaryPdfGenerator pdfGenerator)
    : IQueryHandler<ExportDiaryQuery, Result<FileExportResult>> {
    private const int MaxExportRangeDays = 366;

    public async Task<Result<FileExportResult>> Handle(
        ExportDiaryQuery query,
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

        IReadOnlyList<Meal> meals = await mealRepository.GetByPeriodAsync(
            userId, normalizedFrom, normalizedTo, cancellationToken).ConfigureAwait(false);
        var filteredMeals = meals
            .Where(meal => meal.Date >= normalizedFrom && meal.Date <= normalizedTo)
            .ToList();

        TimeSpan displayOffset = ResolveDisplayOffset(normalizedFrom, query.TimeZoneOffsetMinutes);
        string fromStr = normalizedFrom.Add(displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string toStr = normalizedTo.Add(displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return query.Format switch {
            ExportFormat.Pdf => Result.Success(new FileExportResult(
                await pdfGenerator.GenerateAsync(
                    filteredMeals,
                    query.DateFrom,
                    query.DateTo,
                    query.Locale,
                    query.TimeZoneOffsetMinutes,
                    NormalizeReportOrigin(query.ReportOrigin),
                    cancellationToken).ConfigureAwait(false),
                "application/pdf",
                $"food-diary-{fromStr}-to-{toStr}.pdf")),
            _ => Result.Success(new FileExportResult(
                DiaryCsvGenerator.Generate(filteredMeals, displayOffset),
                "text/csv",
                $"food-diary-{fromStr}-to-{toStr}.csv")),
        };
    }

    private static string? NormalizeReportOrigin(string? reportOrigin) {
        if (string.IsNullOrWhiteSpace(reportOrigin)) {
            return null;
        }

        return Uri.TryCreate(reportOrigin.Trim(), UriKind.Absolute, out Uri? uri)
               && uri.Scheme is "http" or "https"
               && !string.IsNullOrWhiteSpace(uri.Host)
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
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
