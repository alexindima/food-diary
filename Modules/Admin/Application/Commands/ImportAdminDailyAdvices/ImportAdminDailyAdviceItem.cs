namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvices;

public sealed record ImportAdminDailyAdviceItem(string Value, string Locale, int Weight = 1, string? Tag = null);
