namespace FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WaistEntries.Responses;

public sealed record WaistEntryHttpResponse(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double CircumferenceCm);
