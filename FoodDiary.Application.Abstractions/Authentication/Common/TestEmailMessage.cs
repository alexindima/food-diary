namespace FoodDiary.Application.Abstractions.Authentication.Common;

public sealed record TestEmailMessage(
    string ToEmail,
    string? Language);
