using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IFoodRecognitionJobReader {
    Task<FoodRecognitionJobModel?> GetAsync(Guid userId, Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FoodRecognitionJobModel>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FoodRecognitionJobUpdate>> GetUpdatesAsync(DateTime sinceUtc, CancellationToken cancellationToken);
}
