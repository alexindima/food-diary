namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminDailyAdviceImportItemHttpRequest(string Value, string Locale, int Weight = 1, string? Tag = null);
