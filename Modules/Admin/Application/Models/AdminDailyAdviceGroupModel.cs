namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminDailyAdviceGroupModel(Guid Id, string? Ru, string? En, int Weight, string? Tag);
