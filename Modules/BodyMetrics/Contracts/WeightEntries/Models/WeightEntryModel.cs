namespace FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

public sealed record WeightEntryModel(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double WeightKg);
