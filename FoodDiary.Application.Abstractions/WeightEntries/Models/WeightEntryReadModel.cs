namespace FoodDiary.Application.Abstractions.WeightEntries.Models;

public sealed record WeightEntryReadModel(Guid Id, Guid UserId, DateTime Date, double Weight);
