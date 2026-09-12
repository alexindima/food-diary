using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Infrastructure.Persistence.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramOperation {
    public Guid Id { get; init; }
    public long BotId { get; init; }
    public long UpdateId { get; init; }
    public Guid UserId { get; init; }
    public long SecurityVersion { get; init; }
    public required string PayloadHash { get; init; }
    public required string ProtectedPayload { get; set; }
    public string? ProtectedCheckpoint { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public bool Completed { get; set; }
}
