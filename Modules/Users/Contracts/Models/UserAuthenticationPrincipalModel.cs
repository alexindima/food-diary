using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserAuthenticationPrincipalModel(
    UserId UserId,
    string? Email,
    IReadOnlyCollection<string> Roles,
    DateTime? AccessTokenCapUtc,
    UserModel User,
    long SecurityVersion = 0);
