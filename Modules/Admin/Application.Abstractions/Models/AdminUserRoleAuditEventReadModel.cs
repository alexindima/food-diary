namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminUserRoleAuditEventReadModel(
    Guid Id,
    Guid UserId,
    string RoleName,
    string Action,
    Guid? ActorUserId,
    string? ActorEmail,
    string Source,
    DateTime OccurredAtUtc);
