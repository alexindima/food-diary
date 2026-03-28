namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserTokenIssue(
    string TokenHash,
    DateTime ExpiresAtUtc,
    DateTime? IssuedAtUtc = null);
