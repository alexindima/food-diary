namespace FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;

public sealed record WaistEntryModel(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double CircumferenceCm);
