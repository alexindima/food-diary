namespace FoodDiary.Infrastructure.Persistence.Outbox;

/// <summary>A tracked lifecycle record and stream-owned metadata captured before replay.</summary>
public sealed record OutboxReplayEntry(IOutboxMessage Message, string? LastError, string Summary);
