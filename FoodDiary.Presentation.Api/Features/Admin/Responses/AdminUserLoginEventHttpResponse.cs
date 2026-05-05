namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminUserLoginEventHttpResponse(
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

public sealed record AdminUserLoginDeviceSummaryHttpResponse(
    string Key,
    int Count,
    DateTime LastSeenAtUtc);
