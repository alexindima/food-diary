using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Export.Common;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Meals.Common;

namespace FoodDiary.Application.Export.Queries.ExportDiary;

public class ExportDiaryQueryHandler(
    IMealRepository mealRepository,
    IDiaryPdfGenerator pdfGenerator)
    : IQueryHandler<ExportDiaryQuery, Result<FileExportResult>> {
    public async Task<Result<FileExportResult>> Handle(
        ExportDiaryQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FileExportResult>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var normalizedFrom = UtcDateNormalizer.NormalizeDateUsingLocalFallback(query.DateFrom);
        var normalizedTo = UtcDateNormalizer.NormalizeDateEndUsingLocalFallback(query.DateTo);

        var meals = await mealRepository.GetByPeriodAsync(
            userId, normalizedFrom, normalizedTo, cancellationToken);

        var fromStr = query.DateFrom.ToString("yyyy-MM-dd");
        var toStr = query.DateTo.ToString("yyyy-MM-dd");

        return query.Format switch {
            ExportFormat.Pdf => Result.Success(new FileExportResult(
                pdfGenerator.Generate(meals, query.DateFrom, query.DateTo),
                "application/pdf",
                $"food-diary-{fromStr}-to-{toStr}.pdf")),
            _ => Result.Success(new FileExportResult(
                DiaryCsvGenerator.Generate(meals),
                "text/csv",
                $"food-diary-{fromStr}-to-{toStr}.csv")),
        };
    }
}
