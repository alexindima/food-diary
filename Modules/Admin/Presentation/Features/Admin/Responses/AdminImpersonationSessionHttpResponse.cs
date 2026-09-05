namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminImpersonationSessionHttpResponse(
    Guid Id,
    Guid ActorUserId,
    string ActorEmail,
    Guid TargetUserId,
    string TargetEmail,
    string Reason,
    string? ActorIpAddress,
    string? ActorUserAgent,
    DateTime StartedAtUtc);
