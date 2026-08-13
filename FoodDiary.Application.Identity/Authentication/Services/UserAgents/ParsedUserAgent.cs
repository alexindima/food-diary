namespace FoodDiary.Application.Authentication.Services.UserAgents;

internal sealed record ParsedUserAgent(
    string? BrowserName,
    string? BrowserVersion,
    string? OperatingSystem,
    string? DeviceType);
