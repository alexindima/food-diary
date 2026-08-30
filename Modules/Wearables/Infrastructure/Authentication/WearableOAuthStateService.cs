using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.AspNetCore.DataProtection;

namespace FoodDiary.Infrastructure.Authentication;

public sealed class WearableOAuthStateService(
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider) : IWearableOAuthStateService {
    private const string Purpose = "FoodDiary.Wearables.OAuthState.v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    public string CreateState(UserId userId, WearableProvider provider, string? clientState) {
        var payload = new WearableOAuthStatePayload(
            userId.Value,
            provider.ToString(),
            string.IsNullOrWhiteSpace(clientState) ? null : clientState.Trim(),
            Guid.NewGuid().ToString("N"),
            timeProvider.GetUtcNow().UtcDateTime.Add(StateLifetime));

        return _protector.Protect(JsonSerializer.Serialize(payload, JsonOptions));
    }

    public bool IsValidState(string state, UserId userId, WearableProvider provider) {
        if (string.IsNullOrWhiteSpace(state)) {
            return false;
        }

        WearableOAuthStatePayload? payload;
        try {
            string json = _protector.Unprotect(state);
            payload = JsonSerializer.Deserialize<WearableOAuthStatePayload>(json, JsonOptions);
        } catch (CryptographicException) {
            return false;
        } catch (JsonException) {
            return false;
        }

        return payload is not null &&
               payload.UserId == userId.Value &&
               string.Equals(payload.Provider, provider.ToString(), StringComparison.OrdinalIgnoreCase) &&
               payload.ExpiresAtUtc > timeProvider.GetUtcNow().UtcDateTime;
    }

    private sealed record WearableOAuthStatePayload(
        [property: JsonPropertyName("uid")] Guid UserId,
        [property: JsonPropertyName("provider")] string Provider,
        [property: JsonPropertyName("clientState")] string? ClientState,
        [property: JsonPropertyName("nonce")] string Nonce,
        [property: JsonPropertyName("exp")] DateTime ExpiresAtUtc);
}
