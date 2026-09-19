namespace FoodDiary.Modules.DailyAdvices.Contracts.Models;

public sealed record DailyAdviceGroupModel(Guid Id, string? Ru, string? En, int Weight, string? Tag);
