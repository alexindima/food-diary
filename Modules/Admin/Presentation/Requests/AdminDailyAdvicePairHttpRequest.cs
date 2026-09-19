namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminDailyAdvicePairHttpRequest(Guid Id, string Ru, string En, int Weight = 1, string? Tag = null);
