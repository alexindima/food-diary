using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Infrastructure.Persistence.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramLoginTicket {
    public required string Fingerprint { get; init; }
    public required string Purpose { get; init; }
    public required string BrowserBindingHash { get; init; }
    public required string ProtectedPayload { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
}
