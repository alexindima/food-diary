namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class TelegramLoopTelemetry(TelegramLoopKind loop, TimeProvider timeProvider, ILogger logger) {
    private static readonly TimeSpan ReportingInterval = TimeSpan.FromMinutes(1);
    private readonly string _loop = loop switch {
        TelegramLoopKind.Intake => "intake",
        TelegramLoopKind.Discovery => "discovery",
        _ => throw new ArgumentOutOfRangeException(nameof(loop)),
    };
    private long? _lastReport;
    private long _successfulPolls;
    private long _failedPolls;
    private double _durationMilliseconds;
    private long _lastSuccessUnixSeconds;

    public void Observe(bool success, TimeSpan duration) {
        if (success) {
            _successfulPolls++;
            _lastSuccessUnixSeconds = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        } else {
            _failedPolls++;
        }
        _durationMilliseconds += Math.Max(0, duration.TotalMilliseconds);
        long timestamp = timeProvider.GetTimestamp();
        if (_lastReport.HasValue && timeProvider.GetElapsedTime(_lastReport.Value, timestamp) < ReportingInterval) {
            return;
        }
        long averageDurationMilliseconds = (long)(_durationMilliseconds / (_successfulPolls + _failedPolls));
        logger.LogInformation(
            "Telegram loop telemetry {Loop}. SuccessfulPolls={SuccessfulPolls}; FailedPolls={FailedPolls}; AverageDurationMs={AverageDurationMs}; LastSuccessUnixSeconds={LastSuccessUnixSeconds}.",
            _loop, _successfulPolls, _failedPolls, averageDurationMilliseconds, _lastSuccessUnixSeconds);
        _lastReport = timestamp;
        _successfulPolls = 0;
        _failedPolls = 0;
        _durationMilliseconds = 0;
    }
}
