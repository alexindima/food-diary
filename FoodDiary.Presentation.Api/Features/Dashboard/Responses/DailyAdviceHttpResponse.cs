namespace FoodDiary.Presentation.Api.Features.Dashboard.Responses;

public sealed record DailyAdviceHttpResponse(
    Guid Id,
    string Locale,
    string Value,
    string? Tag,
    int Weight);
