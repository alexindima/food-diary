using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Dietologist.Mappings;

public static class DietologistMappings {
    public static DietologistRelationshipModel ToRelationshipModel(this DietologistInvitation invitation) =>
        new(
            invitation.Id.Value,
            invitation.Status.ToString(),
            invitation.DietologistUser?.Email ?? invitation.DietologistEmail,
            invitation.DietologistUser?.FirstName,
            invitation.DietologistUser?.LastName,
            invitation.DietologistUserId?.Value,
            invitation.GetPermissions().ToModel(),
            invitation.CreatedOnUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc);

    public static DietologistInfoModel ToDietologistInfoModel(this DietologistInvitation invitation) =>
        new(
            invitation.Id.Value,
            invitation.DietologistUserId!.Value.Value,
            invitation.DietologistUser!.Email,
            invitation.DietologistUser.FirstName,
            invitation.DietologistUser.LastName,
            invitation.GetPermissions().ToModel(),
            invitation.AcceptedAtUtc!.Value);

    public static ClientSummaryModel ToClientSummaryModel(this DietologistInvitation invitation) =>
        new(
            invitation.ClientUserId.Value,
            invitation.ClientUser.Email,
            invitation.ClientUser.FirstName,
            invitation.ClientUser.LastName,
            invitation.GetPermissions().ToModel(),
            invitation.AcceptedAtUtc!.Value);

    public static InvitationModel ToInvitationModel(this DietologistInvitation invitation) =>
        new(
            invitation.Id.Value,
            invitation.ClientUser.Email,
            invitation.ClientUser.FirstName,
            invitation.ClientUser.LastName,
            invitation.Status.ToString(),
            invitation.CreatedOnUtc,
            invitation.ExpiresAtUtc);

    public static DietologistInvitationForCurrentUserModel ToCurrentUserInvitationModel(this DietologistInvitation invitation) =>
        new(
            invitation.Id.Value,
            invitation.ClientUserId.Value,
            invitation.ClientUser.Email,
            invitation.ClientUser.FirstName,
            invitation.ClientUser.LastName,
            invitation.IsExpired() ? "Expired" : invitation.Status.ToString(),
            invitation.CreatedOnUtc,
            invitation.ExpiresAtUtc);

    public static DietologistPermissions ToPermissions(this DietologistPermissionsInput input) =>
        new(
            input.ShareMeals,
            input.ShareStatistics,
            input.ShareWeight,
            input.ShareWaist,
            input.ShareGoals,
            input.ShareHydration);

    public static DietologistPermissionsModel ToModel(this DietologistPermissions permissions) =>
        new(
            permissions.ShareMeals,
            permissions.ShareStatistics,
            permissions.ShareWeight,
            permissions.ShareWaist,
            permissions.ShareGoals,
            permissions.ShareHydration);

    public static RecommendationModel ToModel(this Recommendation recommendation) =>
        new(
            recommendation.Id.Value,
            recommendation.DietologistUserId.Value,
            recommendation.DietologistUser?.FirstName,
            recommendation.DietologistUser?.LastName,
            recommendation.Text,
            recommendation.IsRead,
            recommendation.CreatedOnUtc,
            recommendation.ReadAtUtc);
}
