using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Options;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace FoodDiary.Modules.Identity.Infrastructure.Providers.Authentication;

public sealed class TelegramAuthValidator(
    IOptions<TelegramAuthOptions> options,
    TimeProvider dateTimeProvider,
    ILogger<TelegramAuthValidator> logger) : ITelegramAuthValidator {
    private static readonly JsonSerializerOptions TelegramUserJsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    private readonly TelegramAuthOptions _options = options.Value;

    public Result<TelegramInitData> ValidateInitData(string initData) {
        if (string.IsNullOrWhiteSpace(initData)) {
            return Reject("missing_init_data", Errors.Validation.Required("initData"));
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken)) {
            return Reject("missing_bot_token", IdentityErrors.TelegramNotConfigured);
        }
        if (!TelegramAuthOptions.HasValidAuthTtl(_options)) {
            return Reject("invalid_auth_ttl", IdentityErrors.TelegramNotConfigured);
        }

        Dictionary<string, StringValues> parsed = QueryHelpers.ParseQuery(initData);
        if (parsed.Any(entry => entry.Value.Count != 1)) {
            return Reject("duplicate_fields", IdentityErrors.TelegramInvalidData);
        }
        if (!parsed.TryGetValue("hash", out StringValues hashValues)) {
            return Reject("missing_hash", IdentityErrors.TelegramInvalidData);
        }

        string hash = hashValues.ToString();
        if (string.IsNullOrWhiteSpace(hash)) {
            return Reject("empty_hash", IdentityErrors.TelegramInvalidData);
        }

        string dataCheckString = BuildDataCheckString(parsed);
        if (!IsValidHash(dataCheckString, hash)) {
            return Reject("hash_mismatch", IdentityErrors.TelegramInvalidData);
        }
        if (!parsed.TryGetValue("auth_date", out StringValues authDateValues)) {
            return Reject("missing_auth_date", IdentityErrors.TelegramInvalidData);
        }
        if (!long.TryParse(authDateValues.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long authDateSeconds)) {
            return Reject("invalid_auth_date", IdentityErrors.TelegramInvalidData);
        }

        TelegramAuthTimestampValidator.Status timestampStatus = TelegramAuthTimestampValidator.Validate(
            authDateSeconds,
            _options.AuthTtlSeconds,
            dateTimeProvider.GetUtcNow().UtcDateTime,
            out DateTime authDateUtc);
        if (timestampStatus == TelegramAuthTimestampValidator.Status.Invalid) {
            return Reject("invalid_timestamp", IdentityErrors.TelegramInvalidData);
        }

        if (timestampStatus == TelegramAuthTimestampValidator.Status.Expired) {
            return Reject("expired_timestamp", IdentityErrors.TelegramAuthExpired);
        }

        if (!parsed.TryGetValue("user", out StringValues userValues)) {
            return Reject("missing_user", IdentityErrors.TelegramInvalidData);
        }

        TelegramWebAppUser? user;
        try {
            user = JsonSerializer.Deserialize<TelegramWebAppUser>(userValues.ToString(), TelegramUserJsonOptions);
        } catch (JsonException) {
            return Reject("invalid_user_json", IdentityErrors.TelegramInvalidData);
        }
        if (user is null) {
            return Reject("invalid_user", IdentityErrors.TelegramInvalidData);
        }
        if (user.Id <= 0) {
            return Reject("invalid_user_id", IdentityErrors.TelegramInvalidData);
        }

        var telegramInitData = new TelegramInitData(
            user.Id,
            user.Username,
            user.FirstName,
            user.LastName,
            user.PhotoUrl,
            user.LanguageCode,
            authDateUtc,
            ReplayIdentity: "miniapp:" + hash,
            ExpiresAtUtc: authDateUtc.AddSeconds(_options.AuthTtlSeconds));

        return Result.Success(telegramInitData);
    }

    private Result<TelegramInitData> Reject(string reason, Error error) {
        // Only constant stage/reason codes belong here; never attach the proof or parser exceptions.
        logger.LogInformation("Telegram Mini App rejected. Stage={Stage}; Reason={Reason}", "init_data", reason);
        return Result.Failure<TelegramInitData>(error);
    }

    private static string BuildDataCheckString(Dictionary<string, StringValues> parsed) {
        IEnumerable<string> pairs = parsed
            .Where(entry => !string.Equals(entry.Key, "hash", StringComparison.Ordinal))
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key}={entry.Value}");

        return string.Join('\n', pairs);
    }

    private bool IsValidHash(string dataCheckString, string hash) {
        byte[] secretKey = ComputeHmacSha256("WebAppData", _options.BotToken);
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
        public long Id { get; init; }

        [JsonPropertyName("username")]
        public string? Username { get; init; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; init; }

        [JsonPropertyName("photo_url")]
        public string? PhotoUrl { get; init; }

        [JsonPropertyName("language_code")]
        public string? LanguageCode { get; init; }
    }
}
