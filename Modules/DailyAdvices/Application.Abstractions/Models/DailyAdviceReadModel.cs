namespace FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;

public sealed record DailyAdviceReadModel(Guid Id, string Locale, string Value, string? Tag, int Weight);
