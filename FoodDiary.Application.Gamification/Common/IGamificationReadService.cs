using FoodDiary.Results;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Common;

public interface IGamificationReadService {
    Task<Result<GamificationModel>> GetAsync(UserId userId, CancellationToken cancellationToken);
}
