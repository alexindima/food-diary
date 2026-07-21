using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Infrastructure.Persistence.Authentication;

[ExcludeFromCodeCoverage]
public sealed class ConsumedTelegramAssertion {
    public required string Fingerprint { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
}
