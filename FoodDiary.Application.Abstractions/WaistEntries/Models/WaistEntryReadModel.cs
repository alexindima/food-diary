namespace FoodDiary.Application.Abstractions.WaistEntries.Models;

public sealed record WaistEntryReadModel(Guid Id, Guid UserId, DateTime Date, double Circumference);
