namespace FoodDiary.JobManager.Services;

public interface IRecurringJobRegistrationVerifier {
    void EnsureRegistered(IReadOnlyCollection<string> expectedJobIds);
}
