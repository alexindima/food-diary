namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserManualPremiumGrantModel(DateTime? EndsAtUtc, string? Reason);
