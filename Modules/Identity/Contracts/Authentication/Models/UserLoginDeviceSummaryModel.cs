namespace FoodDiary.Modules.Identity.Contracts.Authentication.Models;

public sealed record UserLoginDeviceSummaryModel(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
