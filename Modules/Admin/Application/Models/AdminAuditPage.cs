namespace FoodDiary.Application.Admin.Models;

public sealed record AdminAuditPage(IReadOnlyList<AdminAuditEntryModel> Items, int TotalItems);
