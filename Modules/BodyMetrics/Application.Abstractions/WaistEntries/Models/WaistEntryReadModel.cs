namespace FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Models;

public sealed record WaistEntryReadModel(Guid Id, Guid UserId, DateTime Date, double CircumferenceCm);
