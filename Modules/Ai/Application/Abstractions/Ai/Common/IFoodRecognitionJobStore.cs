using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IFoodRecognitionJobStore {
    Task<Result<FoodRecognitionJobModel>> CreateAsync(FoodRecognitionJobModel job, CancellationToken cancellationToken);
    Task<FoodRecognitionJobModel?> GetAsync(Guid userId, Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FoodRecognitionJobModel>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<FoodRecognitionJobModel?> ClaimAsync(CancellationToken cancellationToken);
    Task<bool> SaveVisionAsync(Guid jobId, FoodVisionModel vision, CancellationToken cancellationToken);
    Task CompleteAsync(Guid jobId, FoodNutritionModel? nutrition, string? errorCode, string? nutritionErrorCode, CancellationToken cancellationToken);
    Task MaintainAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<FoodRecognitionJobUpdate>> GetUpdatesAsync(DateTime sinceUtc, CancellationToken cancellationToken);
}
