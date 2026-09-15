using FoodDiary.Modules.Dietologist.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Contracts.Common;

public interface IDietologistDashboardAccessService {
    Task<Result<DietologistPermissionsReadModel>> GetPermissionsAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken = default);
}
