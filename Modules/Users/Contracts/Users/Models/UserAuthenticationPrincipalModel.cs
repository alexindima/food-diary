using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserAuthenticationPrincipalModel(
    UserId UserId,
    string Email,
    IReadOnlyCollection<string> Roles,
    DateTime? AccessTokenCapUtc,
    UserModel User,
    long SecurityVersion = 0);
