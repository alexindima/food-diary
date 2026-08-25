using FoodDiary.Application.Abstractions.Billing.Common;

namespace FoodDiary.Application.Tests.Billing;

[ExcludeFromCodeCoverage]
internal sealed class NoopBillingCheckoutLock : IBillingCheckoutLock {
    public Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IAsyncDisposable>(NoopAsyncDisposable.Instance);

    [ExcludeFromCodeCoverage]
    private sealed class NoopAsyncDisposable : IAsyncDisposable {
        public static readonly NoopAsyncDisposable Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
