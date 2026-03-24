namespace FoodDiary.Application.WeightEntries.Models;

public sealed record WeightEntryModel(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double Weight);
