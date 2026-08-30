namespace FoodDiary.Application.Abstractions.WeightEntries.Models;

public sealed record WeightEntryModel(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double WeightKg);
