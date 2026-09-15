namespace FoodDiary.Modules.Identity.Contracts.Authentication.Common;

public sealed record TestEmailMessage(
    string ToEmail,
    string? Language);
