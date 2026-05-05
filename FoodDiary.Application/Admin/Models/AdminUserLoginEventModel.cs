namespace FoodDiary.Application.Admin.Models;

public sealed record AdminUserLoginEventModel(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string AuthProvider,
    string? MaskedIpAddress,
    string? UserAgent,
    string? BrowserName,
    string? BrowserVersion,
    string? OperatingSystem,
    string? DeviceType,
    DateTime LoggedInAtUtc);

public sealed record AdminUserLoginDeviceSummaryModel(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
