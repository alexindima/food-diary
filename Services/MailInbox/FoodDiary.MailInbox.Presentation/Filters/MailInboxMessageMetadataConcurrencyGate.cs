using FoodDiary.MailInbox.Presentation.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Presentation.Filters;

public sealed class MailInboxMessageMetadataConcurrencyGate(IOptions<MailInboxHttpOptions> options) : IDisposable {
    private readonly SemaphoreSlim _slots = new(
        options.Value.MaxConcurrentMessageMetadataRequests,
        options.Value.MaxConcurrentMessageMetadataRequests);
    private readonly TimeSpan _queueTimeout = options.Value.MessageMetadataQueueTimeout;

    public async ValueTask<IDisposable?> TryEnterAsync(CancellationToken cancellationToken) {
        bool entered = await _slots.WaitAsync(_queueTimeout, cancellationToken).ConfigureAwait(false);
        return entered ? new Lease(_slots) : null;
    }

    public void Dispose() => _slots.Dispose();

    private sealed class Lease(SemaphoreSlim slots) : IDisposable {
        private int _disposed;

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                slots.Release();
            }
        }
    }
}
