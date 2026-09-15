namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DailyAdviceHttpResponse(
    Guid Id,
    string Locale,
    string Value,
    string? Tag,
    int Weight);
