using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Integrations.Authentication;
using FoodDiary.Integrations.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

public sealed class TelegramAuthValidatorsTests {
    private const string BotToken = "123456:test-token";
    private static readonly DateTime NowUtc = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidateInitData_WithValidSignedPayload_ReturnsUser() {
        var authDate = new DateTimeOffset(NowUtc.AddMinutes(-5)).ToUnixTimeSeconds();
        var initData = CreateSignedInitData(authDate);
        var validator = CreateInitDataValidator();

        var result = validator.ValidateInitData(initData);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.UserId);
        Assert.Equal("alex", result.Value.Username);
        Assert.Equal("Alex", result.Value.FirstName);
        Assert.Equal("Doe", result.Value.LastName);
        Assert.Equal("https://example.com/photo.jpg", result.Value.PhotoUrl);
        Assert.Equal("en", result.Value.LanguageCode);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(authDate).UtcDateTime, result.Value.AuthDateUtc);
    }

    [Fact]
    public void ValidateInitData_WithExpiredPayload_ReturnsFailure() {
        var authDate = new DateTimeOffset(NowUtc.AddHours(-2)).ToUnixTimeSeconds();
        var initData = CreateSignedInitData(authDate);
        var validator = CreateInitDataValidator(authTtlSeconds: 60);

        var result = validator.ValidateInitData(initData);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramAuthExpired", result.Error.Code);
    }

    [Fact]
    public void ValidateInitData_WithInvalidHash_ReturnsFailure() {
        var authDate = new DateTimeOffset(NowUtc).ToUnixTimeSeconds();
        var validator = CreateInitDataValidator();

        var result = validator.ValidateInitData($"auth_date={authDate}&user={{\"id\":42}}&hash=bad");

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramInvalidData", result.Error.Code);
    }

    [Fact]
    public void ValidateInitData_WhenBotTokenMissing_ReturnsNotConfigured() {
        var validator = new TelegramAuthValidator(
            MsOptions.Create(new TelegramAuthOptions { BotToken = "" }),
            new FixedDateTimeProvider(NowUtc));

        var result = validator.ValidateInitData("hash=value");

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramNotConfigured", result.Error.Code);
    }

    [Fact]
    public void ValidateLoginWidget_WithValidSignedPayload_ReturnsUser() {
        var authDate = new DateTimeOffset(NowUtc.AddMinutes(-5)).ToUnixTimeSeconds();
        var data = CreateSignedWidgetData(authDate);
        var validator = CreateWidgetValidator();

        var result = validator.ValidateLoginWidget(data);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.UserId);
        Assert.Equal("alex", result.Value.Username);
        Assert.Equal("Alex", result.Value.FirstName);
        Assert.Equal("Doe", result.Value.LastName);
        Assert.Equal("https://example.com/photo.jpg", result.Value.PhotoUrl);
        Assert.Null(result.Value.LanguageCode);
    }

    [Fact]
    public void ValidateLoginWidget_WithExpiredPayload_ReturnsFailure() {
        var authDate = new DateTimeOffset(NowUtc.AddHours(-2)).ToUnixTimeSeconds();
        var data = CreateSignedWidgetData(authDate);
        var validator = CreateWidgetValidator(authTtlSeconds: 60);

        var result = validator.ValidateLoginWidget(data);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramAuthExpired", result.Error.Code);
    }

    [Fact]
    public void ValidateLoginWidget_WithInvalidRequiredFields_ReturnsFailure() {
        var validator = CreateWidgetValidator();

        var result = validator.ValidateLoginWidget(new TelegramLoginWidgetData(0, 0, "", null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramInvalidData", result.Error.Code);
    }

    private static TelegramAuthValidator CreateInitDataValidator(int authTtlSeconds = 3600) {
        return new TelegramAuthValidator(
            MsOptions.Create(new TelegramAuthOptions { BotToken = BotToken, AuthTtlSeconds = authTtlSeconds }),
            new FixedDateTimeProvider(NowUtc));
    }

    private static TelegramLoginWidgetValidator CreateWidgetValidator(int authTtlSeconds = 3600) {
        return new TelegramLoginWidgetValidator(
            MsOptions.Create(new TelegramAuthOptions { BotToken = BotToken, AuthTtlSeconds = authTtlSeconds }),
            new FixedDateTimeProvider(NowUtc));
    }

    private static string CreateSignedInitData(long authDate) {
        var userJson = JsonSerializer.Serialize(new {
            id = 42,
            username = "alex",
            first_name = "Alex",
            last_name = "Doe",
            photo_url = "https://example.com/photo.jpg",
            language_code = "en"
        });
        var dataCheckString = $"auth_date={authDate}\nuser={userJson}";
        var secretKey = ComputeHmacSha256(Encoding.UTF8.GetBytes(BotToken), "WebAppData");
        var hash = ComputeHmacSha256Hex(secretKey, dataCheckString);
        return $"auth_date={authDate}&user={Uri.EscapeDataString(userJson)}&hash={hash}";
    }

    private static TelegramLoginWidgetData CreateSignedWidgetData(long authDate) {
        var dataCheckString = string.Join("\n", [
            $"auth_date={authDate}",
            "first_name=Alex",
            "id=42",
            "last_name=Doe",
            "photo_url=https://example.com/photo.jpg",
            "username=alex"
        ]);
        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(BotToken));
        var hash = ComputeHmacSha256Hex(secretKey, dataCheckString);
        return new TelegramLoginWidgetData(
            42,
            authDate,
            hash.ToUpperInvariant(),
            "alex",
            "Alex",
            "Doe",
            "https://example.com/photo.jpg");
    }

    private static byte[] ComputeHmacSha256(byte[] key, string message) {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }

    private static string ComputeHmacSha256Hex(byte[] key, string message) {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }
}
