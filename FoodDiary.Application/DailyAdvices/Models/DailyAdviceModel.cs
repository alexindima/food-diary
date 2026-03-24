namespace FoodDiary.Application.DailyAdvices.Models;

public sealed record DailyAdviceModel(
    Guid Id,
    string Locale,
    string Value,
    string? Tag,
    int Weight);
