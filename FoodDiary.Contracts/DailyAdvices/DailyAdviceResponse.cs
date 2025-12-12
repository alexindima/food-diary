using System;

namespace FoodDiary.Contracts.DailyAdvices;

public record DailyAdviceResponse(
    Guid Id,
    string Locale,
    string Value,
    string? Tag,
    int Weight);
