namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminAuditEntryHttpResponse(
    Guid Id,
    Guid ActorUserId,
    Guid? SubjectClientUserId,
    string Action,
    string TargetType,
    string? TargetId,
    string? Metadata,
    DateTime CreatedAtUtc);
