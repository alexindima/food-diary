namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminUserLoginEventModel(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string AuthProvider,
    string? MaskedIpAddress,
    string? UserAgent,
    string? BrowserName,
    string? BrowserVersion,
    string? OperatingSystem,
    string? DeviceType,
    DateTime LoggedInAtUtc);
