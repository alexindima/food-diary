using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Export.Models;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Export.Queries.ExportDiary;

public sealed class ExportDiaryQueryHandler(
    IExportDiaryReadService diaryReadService,
    ICurrentUserAccessService currentUserAccessService,
    IDiaryPdfGenerator pdfGenerator)
    : IQueryHandler<ExportDiaryQuery, Result<FileExportResult>> {
    private const int MaxExportRangeDays = 366;
    internal const int MaxCsvMealCount = 10_000;
    internal const int MaxPdfMealCount = 2_000;

    public async Task<Result<FileExportResult>> Handle(
        ExportDiaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<FileExportResult>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        if (!ExportDiaryDateRangePolicy.TryResolve(
                query.DateFrom,
                query.DateTo,
                query.TimeZoneOffsetMinutes,
                out DateTime normalizedFrom,
                out DateTime normalizedTo,
                out TimeSpan displayOffset)) {
            return Result.Failure<FileExportResult>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "Export dates cannot be represented with the requested time-zone offset."));
        }

        if (normalizedFrom > normalizedTo) {
            return Result.Failure<FileExportResult>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be less than or equal to DateTo."));
        }

        if ((normalizedTo - normalizedFrom).TotalDays > MaxExportRangeDays) {
            return Result.Failure<FileExportResult>(
                Errors.Validation.Invalid(nameof(query.DateTo), "Export range must not exceed one year."));
        }

        int mealLimit = query.Format == ExportFormat.Pdf ? MaxPdfMealCount : MaxCsvMealCount;
        ExportDiaryMealsReadModel diary = await diaryReadService.GetMealsAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            mealLimit,
            cancellationToken).ConfigureAwait(false);
        if (diary.HasMore) {
            string mealLimitText = mealLimit.ToString(CultureInfo.InvariantCulture);
            return Result.Failure<FileExportResult>(
                Errors.Validation.Invalid(nameof(query.DateTo), $"Export contains more than {mealLimitText} meals. Choose a shorter date range."));
        }

        string fromStr = normalizedFrom.Add(displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string toStr = normalizedTo.Add(displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return query.Format switch {
            ExportFormat.Pdf => Result.Success(new FileExportResult(
                await pdfGenerator.GenerateAsync(
                    diary.Meals,
                    query.DateFrom,
                    query.DateTo,
                    query.Locale,
                    query.TimeZoneOffsetMinutes,
                    NormalizeReportOrigin(query.ReportOrigin),
                    cancellationToken).ConfigureAwait(false),
                "application/pdf",
                $"food-diary-{fromStr}-to-{toStr}.pdf")),
            _ => Result.Success(new FileExportResult(
                DiaryCsvGenerator.Generate(diary.Meals, displayOffset),
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

}
