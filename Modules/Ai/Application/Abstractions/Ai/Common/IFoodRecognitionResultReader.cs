using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IFoodRecognitionResultReader {
    Task<Result<FoodRecognitionJobModel>> GetCompletedAsync(Guid userId, Guid jobId, CancellationToken cancellationToken);
}
