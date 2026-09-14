namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Requests;

public sealed record CreateWaistEntryHttpRequest(
    DateTime Date,
    double CircumferenceCm);
