namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminAuditEntryModel(
    Guid Id,
    Guid ActorUserId,
    Guid? SubjectClientUserId,
    string Action,
    string TargetType,
    string? TargetId,
    string? Metadata,
    DateTime CreatedAtUtc);
