namespace FoodDiary.Application.Abstractions.Export.Common;

public interface IDiaryPdfReportTextProvider {
    DiaryPdfReportTexts GetTexts(string? locale);
}
