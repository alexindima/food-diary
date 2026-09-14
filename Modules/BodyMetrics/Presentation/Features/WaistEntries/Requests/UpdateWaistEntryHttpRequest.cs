namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Requests;

public sealed record UpdateWaistEntryHttpRequest(
    DateTime Date,
    double CircumferenceCm);
