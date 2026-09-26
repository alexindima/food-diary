namespace FoodDiary.Email.Contracts.Email.Common;

public interface IEmailOutboxProcessor {
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
