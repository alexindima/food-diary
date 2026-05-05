using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Users;

public sealed class UserLoginEvent : Entity<Guid> {
    public UserId UserId { get; private set; }
    public string AuthProvider { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? BrowserName { get; private set; }
    public string? BrowserVersion { get; private set; }
    public string? OperatingSystem { get; private set; }
    public string? DeviceType { get; private set; }
    public DateTime LoggedInAtUtc { get; private set; }

    private UserLoginEvent() {
    }

    public static UserLoginEvent Create(
        UserId userId,
        string authProvider,
        string? ipAddress,
        string? userAgent,
        string? browserName,
        string? browserVersion,
        string? operatingSystem,
        string? deviceType,
        DateTime loggedInAtUtc) {
        if (userId.Value == Guid.Empty) {
            throw new ArgumentException("User id must not be empty.", nameof(userId));
        }

        var loginEvent = new UserLoginEvent {
            Id = Guid.NewGuid(),
            UserId = userId,
            AuthProvider = NormalizeRequiredText(authProvider, nameof(authProvider), 64),
            IpAddress = NormalizeOptionalText(ipAddress, 128),
            UserAgent = NormalizeOptionalText(userAgent, 512),
            BrowserName = NormalizeOptionalText(browserName, 64),
            BrowserVersion = NormalizeOptionalText(browserVersion, 64),
            OperatingSystem = NormalizeOptionalText(operatingSystem, 64),
            DeviceType = NormalizeOptionalText(deviceType, 32),
            LoggedInAtUtc = NormalizeUtcTimestamp(loggedInAtUtc, nameof(loggedInAtUtc))
        };
        loginEvent.SetCreated(loginEvent.LoggedInAtUtc);
        return loginEvent;
    }

    private static string NormalizeRequiredText(string value, string paramName, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? NormalizeOptionalText(string? value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static DateTime NormalizeUtcTimestamp(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }
}
