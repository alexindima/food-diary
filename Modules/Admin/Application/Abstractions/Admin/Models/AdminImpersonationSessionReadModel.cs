namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminImpersonationSessionReadModel(
    Guid Id,
    Guid ActorUserId,
    string ActorEmail,
    Guid TargetUserId,
    string TargetEmail,
    string Reason,
    string? ActorIpAddress,
    string? ActorUserAgent,
    DateTime StartedAtUtc);
