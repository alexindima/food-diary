using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Services;

public sealed class DietologistDashboardAccessService(
    IDietologistInvitationReadModelRepository invitationRepository) : IDietologistDashboardAccessService {
    public async Task<Result<DietologistPermissionsReadModel>> GetPermissionsAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken = default) {
        DietologistInvitationReadModel? invitation = await invitationRepository
            .GetActiveByClientAndDietologistReadModelAsync(clientUserId, dietologistUserId, cancellationToken)
            .ConfigureAwait(false);

        if (invitation is null) {
            return Result.Failure<DietologistPermissionsReadModel>(DietologistErrors.AccessDenied);
        }

        DietologistPermissionsReadModel permissions = invitation.Permissions;
        bool hasDashboardPermission = permissions.ShareMeals ||
                                      permissions.ShareStatistics ||
                                      permissions.ShareWeight ||
                                      permissions.ShareWaist ||
                                      permissions.ShareHydration ||
                                      permissions.ShareFasting;
        return hasDashboardPermission
            ? Result.Success(permissions)
            : Result.Failure<DietologistPermissionsReadModel>(DietologistErrors.PermissionDenied);
    }
}
