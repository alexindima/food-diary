namespace FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WaistEntries.Responses;

public sealed record WaistEntrySummaryHttpResponse(
    DateTime StartDate,
    DateTime EndDate,
    double AverageCircumferenceCm);
