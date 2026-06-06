using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Integrations.Authentication;
using FoodDiary.Integrations.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class TelegramAuthValidatorsTests {
    private const string BotToken = "123456:test-token";
    private static readonly DateTime NowUtc = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidateInitData_WithValidSignedPayload_ReturnsUser() {
        long authDate = new DateTimeOffset(NowUtc.AddMinutes(-5)).ToUnixTimeSeconds();
        string initData = CreateSignedInitData(authDate);
        TelegramAuthValidator validator = CreateInitDataValidator();

        Result<TelegramInitData> result = validator.ValidateInitData(initData);

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
        long authDate = new DateTimeOffset(NowUtc.AddHours(-2)).ToUnixTimeSeconds();
        string initData = CreateSignedInitData(authDate);
        TelegramAuthValidator validator = CreateInitDataValidator(authTtlSeconds: 60);

        Result<TelegramInitData> result = validator.ValidateInitData(initData);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramAuthExpired", result.Error.Code);
    }

    [Fact]
    public void ValidateInitData_WithInvalidHash_ReturnsFailure() {
        long authDate = new DateTimeOffset(NowUtc).ToUnixTimeSeconds();
        TelegramAuthValidator validator = CreateInitDataValidator();

        Result<TelegramInitData> result = validator.ValidateInitData(string.Create(CultureInfo.InvariantCulture, $"auth_date={authDate}&user={{\"id\":42}}&hash=bad"));

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramInvalidData", result.Error.Code);
    }

    [Fact]
    public void ValidateInitData_WithBlankPayload_ReturnsRequiredFailure() {
        TelegramAuthValidator validator = CreateInitDataValidator();

        Result<TelegramInitData> result = validator.ValidateInitData("   ");

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public void ValidateInitData_WithMissingHash_ReturnsInvalidDataFailure() {
        long authDate = new DateTimeOffset(NowUtc).ToUnixTimeSeconds();
        TelegramAuthValidator validator = CreateInitDataValidator();

        Result<TelegramInitData> result = validator.ValidateInitData(string.Create(CultureInfo.InvariantCulture, $"auth_date={authDate}&user={{\"id\":42}}"));

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramInvalidData", result.Error.Code);
    }

    [Fact]
    public void ValidateInitData_WithMalformedUserJson_ReturnsInvalidDataFailure() {
        long authDate = new DateTimeOffset(NowUtc).ToUnixTimeSeconds();
        string initData = CreateSignedInitData(authDate, userJson: "{bad-json");
        TelegramAuthValidator validator = CreateInitDataValidator();

        Result<TelegramInitData> result = validator.ValidateInitData(initData);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramInvalidData", result.Error.Code);
    }

    [Fact]
    public void ValidateInitData_WithInvalidUserId_ReturnsInvalidDataFailure() {
        long authDate = new DateTimeOffset(NowUtc).ToUnixTimeSeconds();
        string initData = CreateSignedInitData(authDate, userJson: "{\"id\":0}");
        TelegramAuthValidator validator = CreateInitDataValidator();

        Result<TelegramInitData> result = validator.ValidateInitData(initData);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramInvalidData", result.Error.Code);
    }

    [Fact]
    public void ValidateInitData_WithZeroTtl_DoesNotExpirePayload() {
        long authDate = new DateTimeOffset(NowUtc.AddDays(-7)).ToUnixTimeSeconds();
        string initData = CreateSignedInitData(authDate);
        TelegramAuthValidator validator = CreateInitDataValidator(authTtlSeconds: 0);

        Result<TelegramInitData> result = validator.ValidateInitData(initData);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.UserId);
    }

    [Fact]
    public void ValidateInitData_WhenBotTokenMissing_ReturnsNotConfigured() {
        var validator = new TelegramAuthValidator(
            MsOptions.Create(new TelegramAuthOptions { BotToken = "" }),
            new FixedDateTimeProvider(NowUtc));

        Result<TelegramInitData> result = validator.ValidateInitData("hash=value");

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramNotConfigured", result.Error.Code);
    }

    [Fact]
    public void ValidateLoginWidget_WithValidSignedPayload_ReturnsUser() {
        long authDate = new DateTimeOffset(NowUtc.AddMinutes(-5)).ToUnixTimeSeconds();
        TelegramLoginWidgetData data = CreateSignedWidgetData(authDate);
        TelegramLoginWidgetValidator validator = CreateWidgetValidator();

        Result<TelegramInitData> result = validator.ValidateLoginWidget(data);

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
        long authDate = new DateTimeOffset(NowUtc.AddHours(-2)).ToUnixTimeSeconds();
        TelegramLoginWidgetData data = CreateSignedWidgetData(authDate);
        TelegramLoginWidgetValidator validator = CreateWidgetValidator(authTtlSeconds: 60);

        Result<TelegramInitData> result = validator.ValidateLoginWidget(data);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramAuthExpired", result.Error.Code);
    }

    [Fact]
    public void ValidateLoginWidget_WithInvalidRequiredFields_ReturnsFailure() {
        TelegramLoginWidgetValidator validator = CreateWidgetValidator();

        Result<TelegramInitData> result = validator.ValidateLoginWidget(new TelegramLoginWidgetData(0, 0, "", Username: null, FirstName: null, LastName: null, PhotoUrl: null));

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramInvalidData", result.Error.Code);
    }

    [Fact]
    public void ValidateLoginWidget_WhenBotTokenMissing_ReturnsNotConfigured() {
        var validator = new TelegramLoginWidgetValidator(
            MsOptions.Create(new TelegramAuthOptions { BotToken = "" }),
            new FixedDateTimeProvider(NowUtc));

        Result<TelegramInitData> result = validator.ValidateLoginWidget(new TelegramLoginWidgetData(42, 1, "hash", Username: null, FirstName: null, LastName: null, PhotoUrl: null));

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramNotConfigured", result.Error.Code);
    }

    [Fact]
    public void ValidateLoginWidget_WithOptionalFieldsMissing_ReturnsUser() {
        long authDate = new DateTimeOffset(NowUtc).ToUnixTimeSeconds();
        string dataCheckString = string.Join("\n", [
            string.Create(CultureInfo.InvariantCulture, $"auth_date={authDate}"),
            "id=42",
        ]);
        byte[] secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(BotToken));
        string hash = ComputeHmacSha256Hex(secretKey, dataCheckString);
        TelegramLoginWidgetValidator validator = CreateWidgetValidator();

        Result<TelegramInitData> result = validator.ValidateLoginWidget(new TelegramLoginWidgetData(42, authDate, hash, Username: null, FirstName: null, LastName: null, PhotoUrl: null));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.UserId);
        Assert.Null(result.Value.Username);
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

    private static string CreateSignedInitData(long authDate, string? userJson = null) {
        userJson ??= JsonSerializer.Serialize(new {
            id = 42,
            username = "alex",
            first_name = "Alex",
            last_name = "Doe",
            photo_url = "https://example.com/photo.jpg",
            language_code = "en",
        });
        string dataCheckString = string.Create(CultureInfo.InvariantCulture, $"auth_date={authDate}\nuser={userJson}");
        byte[] secretKey = ComputeHmacSha256(Encoding.UTF8.GetBytes(BotToken), "WebAppData");
        string hash = ComputeHmacSha256Hex(secretKey, dataCheckString);
        return string.Create(CultureInfo.InvariantCulture, $"auth_date={authDate}&user={Uri.EscapeDataString(userJson)}&hash={hash}");
    }

    private static TelegramLoginWidgetData CreateSignedWidgetData(long authDate) {
        string dataCheckString = string.Join("\n", [
            string.Create(CultureInfo.InvariantCulture, $"auth_date={authDate}"),
            "first_name=Alex",
            "id=42",
            "last_name=Doe",
            "photo_url=https://example.com/photo.jpg",
            "username=alex",
        ]);
        byte[] secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(BotToken));
        string hash = ComputeHmacSha256Hex(secretKey, dataCheckString);
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
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash) {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
