namespace FoodDiary.Application.Abstractions.Audit.Models;

public sealed record AuditEntryReadModel(
    Guid Id,
    Guid ActorUserId,
    Guid? SubjectClientUserId,
    string Action,
    string TargetType,
    string? TargetId,
    string? Metadata,
    DateTime CreatedAtUtc);
