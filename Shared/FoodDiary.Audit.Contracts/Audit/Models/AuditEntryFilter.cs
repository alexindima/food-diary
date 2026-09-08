namespace FoodDiary.Application.Abstractions.Audit.Models;

public sealed record AuditEntryFilter(int Page, int Limit, DateTimeOffset? FromUtc, DateTimeOffset? ToUtc,
    Guid? ActorUserId, Guid? SubjectClientUserId, string? Action, string? TargetType, string? TargetId);
