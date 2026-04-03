namespace FoodDiary.JobManager.Services;

public readonly record struct JobExecutionStateSnapshot(
    DateTime? LastStartedAtUtc,
    DateTime? LastSucceededAtUtc,
    DateTime? LastFailedAtUtc,
    int ConsecutiveFailures);
