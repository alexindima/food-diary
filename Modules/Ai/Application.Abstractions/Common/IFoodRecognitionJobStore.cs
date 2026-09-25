using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IFoodRecognitionJobStore {
    Task<Result<FoodRecognitionJobModel>> CreateAsync(FoodRecognitionJobModel job, CancellationToken cancellationToken);
    Task<FoodRecognitionJobModel?> ClaimAsync(CancellationToken cancellationToken);
    Task<bool> SaveVisionAsync(Guid jobId, FoodVisionModel vision, CancellationToken cancellationToken);
    Task CompleteAsync(Guid jobId, FoodNutritionModel? nutrition, string? errorCode, string? nutritionErrorCode, CancellationToken cancellationToken);
    Task<Result> DeleteCompletedAsync(Guid userId, Guid jobId, CancellationToken cancellationToken);
    Task MaintainAsync(CancellationToken cancellationToken);
}
