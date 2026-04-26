using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public static class DietologistAccessPolicy {
    public static async Task<Result<DietologistPermissionsModel>> EnsureCanAccessClientAsync(
        IDietologistInvitationRepository repository,
        UserId dietologistUserId,
        UserId clientUserId,
        CancellationToken cancellationToken) {
        var invitation = await repository.GetActiveByClientAndDietologistAsync(
            clientUserId, dietologistUserId, cancellationToken);

        if (invitation is null) {
            return Result.Failure<DietologistPermissionsModel>(Errors.Dietologist.AccessDenied);
        }

        var permissions = invitation.GetPermissions();
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
            _ => null
        };
    }
}
