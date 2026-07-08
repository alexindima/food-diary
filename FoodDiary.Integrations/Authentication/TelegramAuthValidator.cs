using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Integrations.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace FoodDiary.Integrations.Authentication;

public sealed class TelegramAuthValidator(IOptions<TelegramAuthOptions> options, TimeProvider dateTimeProvider) : ITelegramAuthValidator {
    private static readonly JsonSerializerOptions TelegramUserJsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    private readonly TelegramAuthOptions _options = options.Value;

    public Result<TelegramInitData> ValidateInitData(string initData) {
        if (string.IsNullOrWhiteSpace(initData)) {
            return Result.Failure<TelegramInitData>(Errors.Validation.Required("initData"));
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramNotConfigured);
        }

        Dictionary<string, StringValues> parsed = QueryHelpers.ParseQuery(initData);
        if (!parsed.TryGetValue("hash", out StringValues hashValues)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        string hash = hashValues.ToString();
        if (string.IsNullOrWhiteSpace(hash)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        string dataCheckString = BuildDataCheckString(parsed);
        if (!IsValidHash(dataCheckString, hash) ||
            !parsed.TryGetValue("auth_date", out StringValues authDateValues) ||
            !long.TryParse(authDateValues.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long authDateSeconds)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        DateTime authDateUtc = DateTimeOffset.FromUnixTimeSeconds(authDateSeconds).UtcDateTime;
        if (_options.AuthTtlSeconds > 0) {
            DateTime expiresAt = authDateUtc.AddSeconds(_options.AuthTtlSeconds);
            if (dateTimeProvider.GetUtcNow().UtcDateTime > expiresAt) {
                return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramAuthExpired);
            }
        }

        if (!parsed.TryGetValue("user", out StringValues userValues)) {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        TelegramWebAppUser? user;
        try {
            user = JsonSerializer.Deserialize<TelegramWebAppUser>(userValues.ToString(), TelegramUserJsonOptions);
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

    private static string BuildDataCheckString(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> parsed) {
        IEnumerable<string> pairs = parsed
            .Where(entry => !string.Equals(entry.Key, "hash", StringComparison.Ordinal))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key}={entry.Value}");

        return string.Join('\n', pairs);
    }

    private bool IsValidHash(string dataCheckString, string hash) {
        byte[] secretKey = ComputeHmacSha256(_options.BotToken, "WebAppData");
        string calculatedHash = ComputeHmacSha256Hex(secretKey, dataCheckString);
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
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash) {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private sealed class TelegramWebAppUser {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("photo_url")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("language_code")]
        public string? LanguageCode { get; set; }
    }
}
