namespace FoodDiary.Application.Abstractions.Audit.Models;

public sealed record AuditEntryPage(IReadOnlyList<AuditEntryReadModel> Items, int TotalItems);
