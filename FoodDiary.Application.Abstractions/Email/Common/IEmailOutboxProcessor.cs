namespace FoodDiary.Application.Abstractions.Email.Common;

public interface IEmailOutboxProcessor {
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
