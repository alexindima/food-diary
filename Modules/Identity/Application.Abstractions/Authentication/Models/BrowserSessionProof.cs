using System.Text.Json.Serialization;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

public sealed record BrowserSessionProof(
    RefreshTokenSessionId Id, UserId UserId, [property: JsonIgnore] string CredentialHash,
    long? SecurityVersion, DateTime? ExpiresAtUtc, DateTime? RevokedAtUtc, DateTime LastActiveAtUtc,
    DateTime CreatedAtUtc = default, bool SlidingExpirationEnabled = false, bool RememberMe = false) {
    public override string ToString() => $"BrowserSessionProof {{ Id = {Id}, CredentialHash = [redacted] }}";
}
