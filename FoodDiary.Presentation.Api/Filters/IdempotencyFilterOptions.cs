namespace FoodDiary.Presentation.Api.Filters;

public sealed class IdempotencyFilterOptions {
    public TimeSpan ResponseTtl { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan ProcessingTtl { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan LeaseRenewalInterval { get; init; } = TimeSpan.FromMinutes(1);

    public TimeSpan StoreOperationTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public static bool IsValid(IdempotencyFilterOptions options) =>
        options.ResponseTtl > TimeSpan.Zero &&
        options.ProcessingTtl > TimeSpan.Zero &&
        options.LeaseRenewalInterval > TimeSpan.Zero &&
        options.StoreOperationTimeout > TimeSpan.Zero &&
        options.StoreOperationTimeout < options.ProcessingTtl &&
        options.LeaseRenewalInterval < options.ProcessingTtl - options.StoreOperationTimeout;
}
