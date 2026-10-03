namespace FoodDiary.Application.Contracts.Common.Abstractions.Persistence;

public sealed record ReadSnapshotBudget(TimeSpan Timeout, int MaximumQueries);
