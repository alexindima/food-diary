namespace FoodDiary.Infrastructure.Persistence.Authentication;

public sealed class ConsumedTelegramAssertion {
    public string Fingerprint { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}
