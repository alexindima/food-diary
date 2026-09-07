using System.Collections.Concurrent;
using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Domain.Emails;

namespace FoodDiary.MailRelay.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class RecordingRelayDeliveryTransport(int remainingFailures = 0) : IRelayDeliveryTransport {
    private readonly ConcurrentQueue<RelayEmailMessageRequest> _sent = new();
    private int _remainingFailures = remainingFailures;
    private int _attemptCount;

    public IReadOnlyCollection<RelayEmailMessageRequest> SentMessages => _sent.ToArray();
    public int AttemptCount => Interlocked.CompareExchange(ref _attemptCount, 0, 0);

    public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        Interlocked.Increment(ref _attemptCount);
        if (Interlocked.CompareExchange(ref _remainingFailures, 0, 0) > 0) {
            Interlocked.Decrement(ref _remainingFailures);
            throw new InvalidOperationException("Simulated relay delivery failure.");
        }

        _sent.Enqueue(request);
        return Task.CompletedTask;
    }
}
