using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Identity.PersistenceModel.Authentication;

[ExcludeFromCodeCoverage]
public sealed class ConsumedTelegramAssertion {
    public required string Fingerprint { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
}
