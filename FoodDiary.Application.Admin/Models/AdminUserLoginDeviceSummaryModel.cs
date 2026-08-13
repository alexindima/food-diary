namespace FoodDiary.Application.Admin.Models;

public sealed record AdminUserLoginDeviceSummaryModel(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
