using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserSessionPrincipalModel(
    UserId UserId, string? Email, IReadOnlyCollection<string> Roles, DateTime? AccessTokenCapUtc, long SecurityVersion);
