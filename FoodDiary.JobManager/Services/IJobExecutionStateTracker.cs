namespace FoodDiary.JobManager.Services;

public interface IJobExecutionStateTracker {
    void RecordStarted(string jobName, DateTime utcNow);
    void RecordSuccess(string jobName, DateTime utcNow);
    void RecordFailure(string jobName, DateTime utcNow);
    JobExecutionStateSnapshot? GetSnapshot(string jobName);
}
