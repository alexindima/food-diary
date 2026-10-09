using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IFoodRecognitionJobReader {
    Task<FoodRecognitionJobModel?> GetAsync(UserId userId, FoodRecognitionJobId jobId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<FoodRecognitionJobModel> Items, int TotalItems)> ListAsync(UserId userId, int page, int limit, bool? isProductLabel, CancellationToken cancellationToken);
    Task<IReadOnlyList<FoodRecognitionJobUpdate>> GetUpdatesAsync(DateTime sinceUtc, CancellationToken cancellationToken);
}
