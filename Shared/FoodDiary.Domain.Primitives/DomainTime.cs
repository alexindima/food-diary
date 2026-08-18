namespace FoodDiary.Domain.Primitives;

public static class DomainTime {
    private static readonly AsyncLocal<TimeProvider?> AmbientTimeProvider = new();

    /// <summary>
    /// Gets the current UTC timestamp from the provider scoped to the current asynchronous execution context.
    /// </summary>
    public static DateTime UtcNow => CurrentTimeProvider.GetUtcNow().UtcDateTime;

    private static TimeProvider CurrentTimeProvider => AmbientTimeProvider.Value ?? TimeProvider.System;

    /// <summary>
    /// Ensures that a timestamp explicitly uses <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    public static DateTime EnsureUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentOutOfRangeException(paramName, "Timestamp must use DateTimeKind.Utc.");
    }

    /// <summary>
    /// Overrides the time provider for the current asynchronous execution context until the returned scope is disposed.
    /// </summary>
    /// <remarks>
    /// Use this for deterministic domain execution, especially in tests. Nested scopes are restored in LIFO order.
    /// </remarks>
    public static IDisposable Override(TimeProvider timeProvider) {
        ArgumentNullException.ThrowIfNull(timeProvider);

        TimeProvider? previous = AmbientTimeProvider.Value;
        AmbientTimeProvider.Value = timeProvider;
        return new TimeProviderOverrideScope(previous);
    }

    private sealed class TimeProviderOverrideScope(TimeProvider? previous) : IDisposable {
        private bool _disposed;

        public void Dispose() {
            if (_disposed) {
                return;
            }

            AmbientTimeProvider.Value = previous;
            _disposed = true;
        }
    }
}
