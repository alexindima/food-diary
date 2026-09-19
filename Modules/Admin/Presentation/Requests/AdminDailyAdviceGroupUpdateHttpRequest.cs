namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminDailyAdviceGroupUpdateHttpRequest(string Ru, string En, int Weight = 1, string? Tag = null);
