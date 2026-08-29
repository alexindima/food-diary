using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Contracts.Read;

public interface IFastingReadService {
    Task<FastingSessionModel?> GetCurrentAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<FastingInsightsModel> GetInsightsAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<FastingOverviewModel> GetOverviewAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
