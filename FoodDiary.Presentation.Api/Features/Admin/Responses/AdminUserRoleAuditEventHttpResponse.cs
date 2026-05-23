namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminUserRoleAuditEventHttpResponse(
    Guid Id,
    Guid UserId,
    string RoleName,
    string Action,
    Guid? ActorUserId,
    string? ActorEmail,
    string Source,
    DateTime OccurredAtUtc);
