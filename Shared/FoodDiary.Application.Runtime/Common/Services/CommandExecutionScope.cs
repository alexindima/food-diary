namespace FoodDiary.Application.Runtime.Common.Services;

/// <summary>Tracks command ownership across mediator requests in one dependency-injection scope.</summary>
public sealed class CommandExecutionScope {
    private int _depth;
    internal bool HasFailedNestedCommand { get; private set; }
    internal void ResetNestedFailures() => HasFailedNestedCommand = false;
    internal void MarkNestedFailure() => HasFailedNestedCommand = true;

    public Lease Enter() {
        bool outermost = _depth++ == 0;
        if (outermost) {
            ResetNestedFailures();
        }
        return new Lease(this, outermost);
    }

    public sealed class Lease(CommandExecutionScope owner, bool isOutermost) : IDisposable {
        public bool IsOutermost { get; } = isOutermost;
        private bool _disposed;

        public void Dispose() {
            if (_disposed) {
                return;
            }
            _disposed = true;
            owner._depth--;
        }
    }
}
