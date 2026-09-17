namespace FoodDiary.Modules.DailyAdvices.Contracts.Models;

public sealed record DailyAdviceImportItem(string Value, string Locale, int Weight = 1, string? Tag = null);
