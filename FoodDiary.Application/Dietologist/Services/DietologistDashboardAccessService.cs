using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Services;

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
            return Result.Failure<DietologistPermissionsReadModel>(Errors.Dietologist.AccessDenied);
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
            : Result.Failure<DietologistPermissionsReadModel>(Errors.Dietologist.PermissionDenied);
    }
}
