using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Contracts.Common;

public interface IFoodRecognitionResultReader {
    Task<Result<FoodRecognitionJobModel>> GetCompletedAsync(Guid userId, Guid jobId, CancellationToken cancellationToken);
}
