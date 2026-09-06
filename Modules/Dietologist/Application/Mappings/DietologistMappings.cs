using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Dietologist.Mappings;

public static class DietologistMappings {
    public static DietologistPermissions ToPermissions(this DietologistPermissionsInput input) =>
        new(
            input.ShareMeals,
            input.ShareStatistics,
            input.ShareWeight,
            input.ShareWaist,
            input.ShareGoals,
            input.ShareHydration,
            input.ShareProfile,
            input.ShareFasting);

    public static DietologistPermissionsModel ToModel(this DietologistPermissions permissions) =>
        new(
            permissions.ShareMeals,
            permissions.ShareStatistics,
            permissions.ShareWeight,
            permissions.ShareWaist,
            permissions.ShareGoals,
            permissions.ShareHydration,
            permissions.ShareProfile,
            permissions.ShareFasting);

    public static DietologistPermissionsModel ToModel(this DietologistPermissionsReadModel permissions) =>
        new(
            permissions.ShareMeals,
            permissions.ShareStatistics,
            permissions.ShareWeight,
            permissions.ShareWaist,
            permissions.ShareGoals,
            permissions.ShareHydration,
            permissions.ShareProfile,
            permissions.ShareFasting);

    public static RecommendationModel ToModel(this Recommendation recommendation) =>
        new(
            recommendation.Id.Value,
            recommendation.DietologistUserId.Value,
            DietologistFirstName: null,
            DietologistLastName: null,
            recommendation.Text,
            recommendation.IsRead,
            recommendation.CreatedOnUtc,
            recommendation.ReadAtUtc);
    public static DietologistRelationshipModel ToRelationshipModel(this DietologistInvitationReadModel invitation) =>
        new(
            invitation.InvitationId,
            invitation.Status.ToString(),
            invitation.DietologistUserEmail ?? invitation.DietologistEmail,
            invitation.DietologistFirstName,
            invitation.DietologistLastName,
            invitation.DietologistUserId,
            invitation.Permissions.ToModel(),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc);

    public static DietologistInfoModel ToDietologistInfoModel(this DietologistInvitationReadModel invitation) =>
        new(
            invitation.InvitationId,
            invitation.DietologistUserId!.Value,
            invitation.DietologistUserEmail!,
            invitation.DietologistFirstName,
            invitation.DietologistLastName,
            invitation.Permissions.ToModel(),
            invitation.AcceptedAtUtc!.Value);

    public static ClientSummaryModel ToClientSummaryModel(this DietologistInvitationReadModel invitation) =>
        new(
            invitation.ClientUserId,
            invitation.ClientEmail,
            invitation.Permissions.ShareProfile ? invitation.ClientFirstName : null,
            invitation.Permissions.ShareProfile ? invitation.ClientLastName : null,
            invitation.Permissions.ShareProfile ? invitation.ClientProfileImage : null,
            invitation.Permissions.ShareProfile ? invitation.ClientBirthDate : null,
            invitation.Permissions.ShareProfile ? invitation.ClientGender : null,
            invitation.Permissions.ShareProfile ? invitation.ClientHeightCm : null,
            invitation.Permissions.ShareProfile ? invitation.ClientActivityLevel.ToString() : null,
            invitation.Permissions.ToModel(),
            invitation.AcceptedAtUtc!.Value);

    public static InvitationModel ToInvitationModel(this DietologistInvitationReadModel invitation) =>
        new(
            invitation.InvitationId,
            invitation.ClientEmail,
            invitation.ClientFirstName,
            invitation.ClientLastName,
            invitation.Status.ToString(),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc);

    public static DietologistInvitationForCurrentUserModel ToCurrentUserInvitationModel(
        this DietologistInvitationReadModel invitation,
        TimeProvider timeProvider) =>
        new(
            invitation.InvitationId,
            invitation.ClientUserId,
            invitation.ClientEmail,
            invitation.ClientFirstName,
            invitation.ClientLastName,
            invitation.Status == DietologistInvitationStatus.Pending &&
                invitation.ExpiresAtUtc <= timeProvider.GetUtcNow().UtcDateTime
                    ? "Expired"
                    : invitation.Status.ToString(),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc);
}
