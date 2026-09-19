namespace FoodDiary.Modules.DailyAdvices.Contracts.Models;

public sealed record DailyAdvicePairImportItem(Guid Id, string Ru, string En, int Weight = 1, string? Tag = null);
