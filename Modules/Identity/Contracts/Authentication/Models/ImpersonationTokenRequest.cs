using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Models;

public sealed record ImpersonationTokenRequest(
    UserId SubjectId,
    string? Email,
    IReadOnlyCollection<string> Roles,
    UserId ActorId,
    string Reason,
    long SecurityVersion);
