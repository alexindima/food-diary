namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminDailyAdviceGroupHttpResponse(Guid Id, string? Ru, string? En, int Weight, string? Tag);
