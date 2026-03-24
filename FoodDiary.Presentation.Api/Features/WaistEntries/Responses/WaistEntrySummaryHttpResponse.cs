namespace FoodDiary.Presentation.Api.Features.WaistEntries.Responses;

public sealed record WaistEntrySummaryHttpResponse(
    DateTime StartDate,
    DateTime EndDate,
    double AverageCircumference);
