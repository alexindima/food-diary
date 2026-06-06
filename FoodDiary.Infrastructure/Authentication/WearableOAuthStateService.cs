using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Authentication;

public sealed class WearableOAuthStateService(
    IOptions<JwtOptions> options,
    IDateTimeProvider dateTimeProvider) : IWearableOAuthStateService {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);
    private readonly JwtOptions _options = options.Value;

    public string CreateState(UserId userId, WearableProvider provider, string? clientState) {
        if (!JwtOptions.HasValidSecretKey(_options)) {
            throw new InvalidOperationException($"{JwtOptions.SectionName}:SecretKey is not configured.");
        }

        var payload = new WearableOAuthStatePayload(
            userId.Value,
            provider.ToString(),
            string.IsNullOrWhiteSpace(clientState) ? null : clientState.Trim(),
            Guid.NewGuid().ToString("N"),
            dateTimeProvider.UtcNow.Add(StateLifetime));
        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        string payloadSegment = Base64UrlEncode(payloadBytes);
        string signatureSegment = Base64UrlEncode(Sign(payloadSegment));

        return $"{payloadSegment}.{signatureSegment}";
    }

    public bool IsValidState(string state, UserId userId, WearableProvider provider) {
        if (!JwtOptions.HasValidSecretKey(_options) || string.IsNullOrWhiteSpace(state)) {
            return false;
        }

        string[] parts = state.Split('.', 2);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1])) {
            return false;
        }

        byte[] expectedSignature = Sign(parts[0]);
        byte[] providedSignature;
        try {
            providedSignature = Base64UrlDecode(parts[1]);
        } catch (FormatException) {
            return false;
        }

        if (providedSignature.Length != expectedSignature.Length ||
            !CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature)) {
            return false;
        }

        WearableOAuthStatePayload? payload;
        try {
            payload = JsonSerializer.Deserialize<WearableOAuthStatePayload>(Base64UrlDecode(parts[0]), JsonOptions);
        } catch (JsonException) {
            return false;
        } catch (FormatException) {
            return false;
        }

        return payload is not null &&
               payload.UserId == userId.Value &&
               string.Equals(payload.Provider, provider.ToString(), StringComparison.OrdinalIgnoreCase) &&
               payload.ExpiresAtUtc > dateTimeProvider.UtcNow;
    }

    private byte[] Sign(string payloadSegment) {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadSegment));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value) {
        string base64 = value.Replace('-', '+').Replace('_', '/');
        int padding = base64.Length % 4;
        if (padding > 0) {
            base64 = base64.PadRight(base64.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(base64);
    }

    private sealed record WearableOAuthStatePayload(
        [property: JsonPropertyName("uid")] Guid UserId,
        [property: JsonPropertyName("provider")] string Provider,
        [property: JsonPropertyName("clientState")] string? ClientState,
        [property: JsonPropertyName("nonce")] string Nonce,
        [property: JsonPropertyName("exp")] DateTime ExpiresAtUtc);
}
