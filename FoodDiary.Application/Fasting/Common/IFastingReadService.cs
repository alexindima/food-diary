using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Common;

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
