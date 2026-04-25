using System.Collections.Concurrent;
using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Emails.Models;

namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

public sealed class RecordingRelayDeliveryTransport : IRelayDeliveryTransport {
    private readonly ConcurrentQueue<RelayEmailMessageRequest> _sent = new();
    private int _remainingFailures;

    public RecordingRelayDeliveryTransport(int remainingFailures = 0) {
        _remainingFailures = remainingFailures;
    }

    public IReadOnlyCollection<RelayEmailMessageRequest> SentMessages => _sent.ToArray();

    public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        if (Interlocked.CompareExchange(ref _remainingFailures, 0, 0) > 0) {
            Interlocked.Decrement(ref _remainingFailures);
            throw new InvalidOperationException("Simulated relay delivery failure.");
        }

        _sent.Enqueue(request);
        return Task.CompletedTask;
    }
}
