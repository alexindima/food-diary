namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminDailyAdviceHttpResponse(Guid Id, string Locale, string Value, string? Tag, int Weight);
