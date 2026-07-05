using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public static class DietologistAccessPolicy {
    public static async Task<Result<DietologistPermissionsModel>> EnsureCanAccessClientAsync(
        IDietologistInvitationReadRepository repository,
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken) {
        DietologistInvitation? invitation = await repository.GetActiveByClientAndDietologistAsync(
            clientUserId, dietologistUserId, cancellationToken).ConfigureAwait(false);

        if (invitation is null) {
            return Result.Failure<DietologistPermissionsModel>(Errors.Dietologist.AccessDenied);
        }

        DietologistPermissions permissions = invitation.GetPermissions();
        return Result.Success(new DietologistPermissionsModel(
            permissions.ShareMeals,
            permissions.ShareStatistics,
            permissions.ShareWeight,
            permissions.ShareWaist,
            permissions.ShareGoals,
            permissions.ShareHydration,
            permissions.ShareProfile,
            permissions.ShareFasting));
    }

    public static async Task<Result<DietologistPermissionsModel>> EnsureCanAccessClientReadModelAsync(
        IDietologistInvitationReadRepository repository,
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken) {
        DietologistInvitationReadModel? invitation = await repository.GetActiveByClientAndDietologistReadModelAsync(
            clientUserId, dietologistUserId, cancellationToken).ConfigureAwait(false);

        return invitation is null
            ? Result.Failure<DietologistPermissionsModel>(Errors.Dietologist.AccessDenied)
            : Result.Success(invitation.Permissions.ToApplicationModel());
    }

    public static Error? EnsurePermission(DietologistPermissionsModel permissions, string category) {
        return category switch {
            "Profile" when !permissions.ShareProfile => Errors.Dietologist.PermissionDenied,
            "Meals" when !permissions.ShareMeals => Errors.Dietologist.PermissionDenied,
            "Statistics" when !permissions.ShareStatistics => Errors.Dietologist.PermissionDenied,
            "Weight" when !permissions.ShareWeight => Errors.Dietologist.PermissionDenied,
            "Waist" when !permissions.ShareWaist => Errors.Dietologist.PermissionDenied,
            "Goals" when !permissions.ShareGoals => Errors.Dietologist.PermissionDenied,
            "Hydration" when !permissions.ShareHydration => Errors.Dietologist.PermissionDenied,
            "Fasting" when !permissions.ShareFasting => Errors.Dietologist.PermissionDenied,
            "Profile" or "Meals" or "Statistics" or "Weight" or "Waist" or "Goals" or "Hydration" or "Fasting" => null,
            _ => Errors.Dietologist.PermissionDenied,
        };
    }

    public static bool HasAnyDashboardPermission(DietologistPermissionsModel permissions) {
        return permissions.ShareMeals ||
               permissions.ShareStatistics ||
               permissions.ShareWeight ||
               permissions.ShareWaist ||
               permissions.ShareHydration ||
               permissions.ShareFasting;
    }

    public static Error? EnsureAllPermissions(DietologistPermissionsModel permissions) {
        return permissions is { ShareMeals: true, ShareStatistics: true, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true, ShareProfile: true, ShareFasting: true }
            ? null
            : Errors.Dietologist.PermissionDenied;
    }

    public static DietologistPermissionsModel ToApplicationModel(this DietologistPermissionsReadModel permissions) =>
        new(
            permissions.ShareMeals,
            permissions.ShareStatistics,
            permissions.ShareWeight,
            permissions.ShareWaist,
            permissions.ShareGoals,
            permissions.ShareHydration,
            permissions.ShareProfile,
            permissions.ShareFasting);
}
