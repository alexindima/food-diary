namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminAuditPage(IReadOnlyList<AdminAuditEntryModel> Items, int TotalItems);
