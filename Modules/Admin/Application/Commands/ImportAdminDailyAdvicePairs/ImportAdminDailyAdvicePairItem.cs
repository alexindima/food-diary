namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvicePairs;

public sealed record ImportAdminDailyAdvicePairItem(Guid Id, string Ru, string En, int Weight = 1, string? Tag = null);
