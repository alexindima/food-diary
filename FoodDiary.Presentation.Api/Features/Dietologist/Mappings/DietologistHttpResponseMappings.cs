using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Mappings;

public static class DietologistHttpResponseMappings {
    public static DietologistInvitationForCurrentUserHttpResponse ToHttpResponse(this DietologistInvitationForCurrentUserModel model) =>
        new(
            model.InvitationId,
            model.ClientUserId,
            model.ClientEmail,
            model.ClientFirstName,
            model.ClientLastName,
            model.Status,
            model.CreatedAtUtc,
            model.ExpiresAtUtc);

    public static DietologistRelationshipHttpResponse ToHttpResponse(this DietologistRelationshipModel model) =>
        new(
            model.InvitationId,
            model.Status,
            model.Email,
            model.FirstName,
            model.LastName,
            model.DietologistUserId,
            model.Permissions.ToHttpResponse(),
            model.CreatedAtUtc,
            model.ExpiresAtUtc,
            model.AcceptedAtUtc);

    public static DietologistInfoHttpResponse ToHttpResponse(this DietologistInfoModel model) =>
        new(model.InvitationId, model.DietologistUserId, model.Email,
            model.FirstName, model.LastName,
            model.Permissions.ToHttpResponse(), model.AcceptedAtUtc);

    public static ClientSummaryHttpResponse ToHttpResponse(this ClientSummaryModel model) =>
        new(model.UserId, model.Email, model.FirstName, model.LastName,
            model.ProfileImage, model.BirthDate, model.Gender, model.Height, model.ActivityLevel,
            model.Permissions.ToHttpResponse(), model.AcceptedAtUtc);

    public static InvitationHttpResponse ToHttpResponse(this InvitationModel model) =>
        new(model.InvitationId, model.ClientEmail, model.ClientFirstName,
            model.ClientLastName, model.Status, model.CreatedAtUtc, model.ExpiresAtUtc);

    public static DietologistPermissionsHttpResponse ToHttpResponse(this DietologistPermissionsModel permissions) =>
        new(permissions.ShareMeals, permissions.ShareStatistics, permissions.ShareWeight,
            permissions.ShareWaist, permissions.ShareGoals, permissions.ShareHydration, permissions.ShareProfile, permissions.ShareFasting);

    public static RecommendationHttpResponse ToHttpResponse(this RecommendationModel model) =>
        new(model.Id, model.DietologistUserId, model.DietologistFirstName,
            model.DietologistLastName, model.Text, model.IsRead,
            model.CreatedAtUtc, model.ReadAtUtc);
}
