using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Mappings;

public static class DietologistHttpResponseMappings {
    public static DietologistInfoHttpResponse ToHttpResponse(this DietologistInfoModel model) =>
        new(model.InvitationId, model.DietologistUserId, model.Email,
            model.FirstName, model.LastName,
            model.Permissions.ToHttpResponse(), model.AcceptedAtUtc);

    public static ClientSummaryHttpResponse ToHttpResponse(this ClientSummaryModel model) =>
        new(model.UserId, model.Email, model.FirstName, model.LastName,
            model.Permissions.ToHttpResponse(), model.AcceptedAtUtc);

    public static InvitationHttpResponse ToHttpResponse(this InvitationModel model) =>
        new(model.InvitationId, model.ClientEmail, model.ClientFirstName,
            model.ClientLastName, model.Status, model.CreatedAtUtc, model.ExpiresAtUtc);

    public static DietologistPermissionsHttpResponse ToHttpResponse(this DietologistPermissionsModel permissions) =>
        new(permissions.ShareMeals, permissions.ShareStatistics, permissions.ShareWeight,
            permissions.ShareWaist, permissions.ShareGoals, permissions.ShareHydration);
}
