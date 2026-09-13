namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminUserLoginDeviceSummaryModel(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
