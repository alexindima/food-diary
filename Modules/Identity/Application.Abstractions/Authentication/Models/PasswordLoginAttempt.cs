namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

public sealed record PasswordLoginAttempt(string Key, string? OwnerToken, int RetryAfterSeconds = 0) {
    public bool IsAcquired => OwnerToken is not null;
}
