namespace FoodDiary.Application.Abstractions.Authentication.Models;

public sealed record UserLoginDeviceSummaryModel(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
