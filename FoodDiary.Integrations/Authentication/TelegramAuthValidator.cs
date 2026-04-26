using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Integrations.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Authentication;

public sealed class TelegramAuthValidator(IOptions<TelegramAuthOptions> options, IDateTimeProvider dateTimeProvider) : ITelegramAuthValidator {
    private readonly TelegramAuthOptions _options = options.Value;

    public Result<TelegramInitData> ValidateInitData(string initData) {
        if (string.IsNullOrWhiteSpace(initData)) {
            return Result.Failure<TelegramInitData>(Errors.Validation.Required("initData"));
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramNotConfigured);
        }

        var parsed = QueryHelpers.ParseQuery(initData);
        if (!parsed.TryGetValue("hash", out var hashValues)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        var hash = hashValues.ToString();
        if (string.IsNullOrWhiteSpace(hash)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        var dataCheckString = BuildDataCheckString(parsed);
        if (!IsValidHash(dataCheckString, hash) ||
            !parsed.TryGetValue("auth_date", out var authDateValues) ||
            !long.TryParse(authDateValues.ToString(), out var authDateSeconds)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        var authDateUtc = DateTimeOffset.FromUnixTimeSeconds(authDateSeconds).UtcDateTime;
        if (_options.AuthTtlSeconds > 0) {
            var expiresAt = authDateUtc.AddSeconds(_options.AuthTtlSeconds);
            if (dateTimeProvider.UtcNow > expiresAt) {
                return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramAuthExpired);
            }
        }

        if (!parsed.TryGetValue("user", out var userValues)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        TelegramWebAppUser? user;
        try {
            user = JsonSerializer.Deserialize<TelegramWebAppUser>(userValues.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        } catch {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        if (user is not { Id: > 0 }) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        var telegramInitData = new TelegramInitData(
            user.Id,
            user.Username,
            user.FirstName,
            user.LastName,
            user.PhotoUrl,
            user.LanguageCode,
            authDateUtc);

        return Result.Success(telegramInitData);
    }

    private string BuildDataCheckString(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> parsed) {
        var pairs = parsed
            .Where(entry => entry.Key != "hash")
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key}={entry.Value}");

        return string.Join("\n", pairs);
    }

    private bool IsValidHash(string dataCheckString, string hash) {
        var secretKey = ComputeHmacSha256(_options.BotToken, "WebAppData");
        var calculatedHash = ComputeHmacSha256Hex(secretKey, dataCheckString);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(calculatedHash),
            Encoding.UTF8.GetBytes(hash));
    }

    private static byte[] ComputeHmacSha256(string key, string message) {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }

    private static string ComputeHmacSha256Hex(byte[] key, string message) {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    private sealed class TelegramWebAppUser {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhotoUrl { get; set; }
        public string? LanguageCode { get; set; }
    }
}
