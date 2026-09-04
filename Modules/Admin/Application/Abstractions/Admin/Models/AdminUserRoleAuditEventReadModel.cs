namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminUserRoleAuditEventReadModel(
    Guid Id,
    Guid UserId,
    string RoleName,
    string Action,
    Guid? ActorUserId,
    string? ActorEmail,
    string Source,
    DateTime OccurredAtUtc);
