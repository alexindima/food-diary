using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IFoodRecognitionJobStore {
    Task<Result<FoodRecognitionJobModel>> CreateAsync(FoodRecognitionJobModel job, CancellationToken cancellationToken);
    Task<FoodRecognitionJobModel?> ClaimAsync(CancellationToken cancellationToken);
    Task<bool> SaveVisionAsync(FoodRecognitionJobId jobId, FoodVisionModel vision, CancellationToken cancellationToken);
    Task CompleteAsync(FoodRecognitionJobId jobId, FoodNutritionModel? nutrition, string? errorCode, string? nutritionErrorCode, CancellationToken cancellationToken);
    Task<Result> DeleteCompletedAsync(UserId userId, FoodRecognitionJobId jobId, CancellationToken cancellationToken);
    Task MaintainAsync(CancellationToken cancellationToken);
}
