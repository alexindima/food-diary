namespace FoodDiary.Modules.Identity.Contracts.Authentication.Models;

public sealed record UserLoginEventReadModel(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string AuthProvider,
    string? IpAddress,
    string? UserAgent,
    string? BrowserName,
    string? BrowserVersion,
    string? OperatingSystem,
    string? DeviceType,
    DateTime LoggedInAtUtc);
