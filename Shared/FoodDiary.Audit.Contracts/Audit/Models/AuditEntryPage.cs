namespace FoodDiary.Audit.Contracts.Audit.Models;

public sealed record AuditEntryPage(IReadOnlyList<AuditEntryReadModel> Items, int TotalItems);
