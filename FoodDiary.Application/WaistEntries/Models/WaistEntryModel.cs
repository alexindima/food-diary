namespace FoodDiary.Application.WaistEntries.Models;

public sealed record WaistEntryModel(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double Circumference);
