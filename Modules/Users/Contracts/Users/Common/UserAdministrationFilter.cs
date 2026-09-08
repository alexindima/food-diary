namespace FoodDiary.Application.Abstractions.Users.Common;

public sealed record UserAdministrationFilter(
    DateOnly? RegisteredFrom = null,
    DateOnly? RegisteredTo = null,
    string? Role = null,
    bool? EmailConfirmed = null,
    DateOnly? LastLoginFrom = null,
    DateOnly? LastLoginTo = null);
