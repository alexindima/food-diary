namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminDailyAdviceModel(Guid Id, string Locale, string Value, string? Tag, int Weight);
