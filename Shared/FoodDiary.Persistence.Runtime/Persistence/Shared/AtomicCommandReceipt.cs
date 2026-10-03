namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

public sealed class AtomicCommandReceipt {
    public Guid UserId { get; init; }
    public required string Key { get; init; }
    public required string RequestHash { get; init; }
    public required string ResponseType { get; init; }
    public required string ResponseJson { get; init; }
    public DateTime ExpiresOnUtc { get; init; }
}
