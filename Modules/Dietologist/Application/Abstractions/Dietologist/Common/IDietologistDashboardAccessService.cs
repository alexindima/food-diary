using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IDietologistDashboardAccessService {
    Task<Result<DietologistPermissionsReadModel>> GetPermissionsAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken = default);
}
