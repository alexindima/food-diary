namespace FoodDiary.Development.Mcp.Wiki;

public sealed record WikiRuntimeMetrics(
    WikiQueryCacheMetrics QueryCache,
    int ActiveCommands,
    int QueuedCommands,
    long CompletedCommands,
    long FailedCommands,
    long CancelledCommands,
    long TimedOutCommands,
    IReadOnlyList<WikiCommandTiming> CommandTimings,
    IReadOnlyList<WikiCommandStageTiming> CommandStageTimings);
