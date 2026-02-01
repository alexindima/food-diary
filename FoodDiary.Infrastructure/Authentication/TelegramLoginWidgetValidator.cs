using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Authentication;

public sealed class TelegramLoginWidgetValidator : ITelegramLoginWidgetValidator
{
    private readonly TelegramAuthOptions _options;

    public TelegramLoginWidgetValidator(IOptions<TelegramAuthOptions> options)
    {
        _options = options.Value;
    }

    public Result<TelegramInitData> ValidateLoginWidget(TelegramLoginWidgetData data)
    {
        if (data.Id <= 0 || data.AuthDate <= 0 || string.IsNullOrWhiteSpace(data.Hash))
        {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramNotConfigured);
        }

        var dataCheckString = BuildDataCheckString(data);
        if (!IsValidHash(dataCheckString, data.Hash))
        {
            return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramInvalidData);
        }

        var authDateUtc = DateTimeOffset.FromUnixTimeSeconds(data.AuthDate).UtcDateTime;
        if (_options.AuthTtlSeconds > 0)
        {
            var expiresAt = authDateUtc.AddSeconds(_options.AuthTtlSeconds);
            if (DateTime.UtcNow > expiresAt)
            {
                return Result.Failure<TelegramInitData>(Errors.Authentication.TelegramAuthExpired);
            }
        }

        var telegramInitData = new TelegramInitData(
            data.Id,
            data.Username,
            data.FirstName,
            data.LastName,
            data.PhotoUrl,
            null,
            authDateUtc);

        return Result.Success(telegramInitData);
    }

    private static string BuildDataCheckString(TelegramLoginWidgetData data)
    {
        var pairs = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = data.AuthDate.ToString(),
            ["id"] = data.Id.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(data.FirstName))
        {
            pairs["first_name"] = data.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(data.LastName))
        {
            pairs["last_name"] = data.LastName;
        }

        if (!string.IsNullOrWhiteSpace(data.Username))
        {
            pairs["username"] = data.Username;
        }

        if (!string.IsNullOrWhiteSpace(data.PhotoUrl))
        {
            pairs["photo_url"] = data.PhotoUrl;
        }

        return string.Join("\n", pairs.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private bool IsValidHash(string dataCheckString, string hash)
    {
        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(_options.BotToken));
        var calculatedHash = ComputeHmacSha256Hex(secretKey, dataCheckString);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(calculatedHash),
            Encoding.UTF8.GetBytes(hash.ToLowerInvariant()));
    }

    private static string ComputeHmacSha256Hex(byte[] key, string message)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
