namespace FoodDiary.Application.Abstractions.DailyAdvices.Models;

public sealed record DailyAdviceReadModel(Guid Id, string Locale, string Value, string? Tag, int Weight);
