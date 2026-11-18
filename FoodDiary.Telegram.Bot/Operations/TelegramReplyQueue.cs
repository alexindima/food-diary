using System.Collections.Concurrent;
using System.Threading.Channels;
using Telegram.Bot.Exceptions;

namespace FoodDiary.Telegram.Bot.Operations;

public sealed class TelegramReplyQueue(TimeProvider timeProvider, ILogger<TelegramReplyQueue> logger) : BackgroundService {
    private readonly Channel<Reply> _queue = Channel.CreateBounded<Reply>(128);
    private readonly ConcurrentDictionary<long, byte> _pendingSenders = new();

    internal bool Enqueue(long sender, Func<CancellationToken, Task> send) {
        if (!_pendingSenders.TryAdd(sender, 0)) {
            return false;
        }
        if (_queue.Writer.TryWrite(new Reply(sender, send))) {
            return true;
        }
        _pendingSenders.TryRemove(sender, out _);
        logger.LogInformation("Telegram reply queue is full; the chat reply was dropped.");
        return false;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(Enumerable.Range(0, 4).Select(_ => RunAsync(stoppingToken)));

    private async Task RunAsync(CancellationToken cancellationToken) {
        try {
            while (await _queue.Reader.WaitToReadAsync(cancellationToken)) {
                await ProcessNextAsync(cancellationToken);
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Host shutdown cancels pending chat replies independently of durable intake.
        }
    }

    internal async Task<bool> ProcessNextAsync(CancellationToken cancellationToken) {
        if (!_queue.Reader.TryRead(out Reply? reply)) {
            return false;
        }
        try {
            for (int attempt = 0; attempt < 3; attempt++) {
                try {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10), timeProvider);
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                    await reply.Send(linked.Token);
                    // Keep the sender's slot occupied to bound repeated ordinary replies.
                    await Task.Delay(TimeSpan.FromSeconds(1), timeProvider, cancellationToken);
                    return true;
                } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    throw;
                } catch (ApiRequestException error) when (error.ErrorCode is 400 or 403) {
                    logger.LogInformation("Telegram permanently rejected a chat reply with {ErrorCode}.", error.ErrorCode);
                    return true;
                } catch (Exception error) {
                    logger.LogWarning("Telegram chat reply attempt failed with {ErrorType}.", error.GetType().Name);
                    if (attempt < 2) {
                        await Task.Delay(TimeSpan.FromSeconds(2 << attempt), timeProvider, cancellationToken);
                    }
                }
            }
            return true;
        } finally {
            _pendingSenders.TryRemove(reply.Sender, out _);
        }
    }

    private sealed record Reply(long Sender, Func<CancellationToken, Task> Send);
}
