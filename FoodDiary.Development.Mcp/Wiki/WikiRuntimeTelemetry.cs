using System.Collections.Concurrent;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class WikiRuntimeTelemetry {
    private const int MaximumTimingSamples = 128;
    private readonly ConcurrentDictionary<string, TimingWindow> _timings =
        new(StringComparer.OrdinalIgnoreCase);
    private long _cacheHits;
    private long _cacheMisses;
    private long _activeCommands;
    private long _queuedCommands;
    private long _completedCommands;
    private long _failedCommands;
    private long _cancelledCommands;
    private long _timedOutCommands;

    public void RecordCacheHit() => Interlocked.Increment(ref _cacheHits);

    public void RecordCacheMiss() => Interlocked.Increment(ref _cacheMisses);

    public void CommandQueued() => Interlocked.Increment(ref _queuedCommands);

    public void CommandQueueCancelled() {
        Interlocked.Decrement(ref _queuedCommands);
        Interlocked.Increment(ref _cancelledCommands);
    }

    public void CommandStarted() {
        Interlocked.Decrement(ref _queuedCommands);
        Interlocked.Increment(ref _activeCommands);
    }

    public void CommandCompleted(string command, TimeSpan duration) {
        Interlocked.Decrement(ref _activeCommands);
        Interlocked.Increment(ref _completedCommands);
        _timings.GetOrAdd(command, static _ => new TimingWindow())
            .Add(duration.TotalMilliseconds);
    }

    public void CommandFailed(bool cancelled, bool timedOut) {
        Interlocked.Decrement(ref _activeCommands);
        if (timedOut) {
            Interlocked.Increment(ref _timedOutCommands);
        } else if (cancelled) {
            Interlocked.Increment(ref _cancelledCommands);
        } else {
            Interlocked.Increment(ref _failedCommands);
        }
    }

    public WikiRuntimeMetrics Capture(int cacheEntries) {
        long hits = Interlocked.Read(ref _cacheHits);
        long misses = Interlocked.Read(ref _cacheMisses);
        long attempts = hits + misses;
        return new WikiRuntimeMetrics(
            new WikiQueryCacheMetrics(
                cacheEntries,
                hits,
                misses,
                attempts == 0 ? 0 : Math.Round(
                    (double)hits / attempts,
                    4,
                    MidpointRounding.AwayFromZero)),
            checked((int)Interlocked.Read(ref _activeCommands)),
            checked((int)Interlocked.Read(ref _queuedCommands)),
            Interlocked.Read(ref _completedCommands),
            Interlocked.Read(ref _failedCommands),
            Interlocked.Read(ref _cancelledCommands),
            Interlocked.Read(ref _timedOutCommands),
            [.. _timings
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => pair.Value.Capture(pair.Key))]);
    }

    private sealed class TimingWindow {
        private readonly Lock _sync = new();
        private readonly Queue<double> _samples = new();

        public void Add(double milliseconds) {
            lock (_sync) {
                _samples.Enqueue(milliseconds);
                while (_samples.Count > MaximumTimingSamples) {
                    _samples.Dequeue();
                }
            }
        }

        public WikiCommandTiming Capture(string command) {
            double[] samples;
            lock (_sync) {
                samples = [.. _samples.Order()];
            }
            return new WikiCommandTiming(
                command,
                samples.Length,
                Percentile(samples, 0.50),
                Percentile(samples, 0.95),
                samples.Length == 0 ? 0 : Math.Round(
                    samples[^1],
                    2,
                    MidpointRounding.AwayFromZero));
        }

        private static double Percentile(IReadOnlyList<double> samples, double percentile) {
            if (samples.Count == 0) {
                return 0;
            }
            int index = (int)Math.Ceiling(percentile * samples.Count) - 1;
            return Math.Round(
                samples[Math.Clamp(index, 0, samples.Count - 1)],
                2,
                MidpointRounding.AwayFromZero);
        }
    }
}
