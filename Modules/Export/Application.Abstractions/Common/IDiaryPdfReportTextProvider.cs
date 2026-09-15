namespace FoodDiary.Modules.Export.Application.Abstractions.Common;

public interface IDiaryPdfReportTextProvider {
    DiaryPdfReportTexts GetTexts(string? locale);
}
