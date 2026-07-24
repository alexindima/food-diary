using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Mappings;

public static class DietologistHttpResponseMappings {
    public static AttentionSignalHttpResponse ToHttpResponse(this AttentionSignalModel model) =>
        new(
            model.Id,
            model.ClientUserId,
            model.ClientDisplayName,
            model.Type,
            model.Severity,
            model.Reason,
            model.DetectedAtUtc,
            model.SnoozedUntilUtc);

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

    public static DietologistRelationshipHttpResponse ToHttpResponse(this ProfileDietologistRelationshipModel model) =>
        new(
            model.InvitationId,
            model.Status,
            model.Email,
            model.FirstName,
            model.LastName,
            model.DietologistUserId,
            new DietologistPermissionsHttpResponse(
                model.Permissions.ShareMeals,
                model.Permissions.ShareStatistics,
                model.Permissions.ShareWeight,
                model.Permissions.ShareWaist,
                model.Permissions.ShareGoals,
                model.Permissions.ShareHydration,
                model.Permissions.ShareProfile,
                model.Permissions.ShareFasting),
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

    public static RecommendationCommentHttpResponse ToHttpResponse(this RecommendationCommentModel model) =>
        new(model.Id, model.RecommendationId, model.AuthorUserId,
            model.AuthorFirstName, model.AuthorLastName, model.AuthorEmail,
            model.Text, model.CreatedAtUtc);

    public static ClientTaskHttpResponse ToHttpResponse(this ClientTaskModel model) =>
        new(
            model.Id,
            model.DietologistUserId,
            model.ClientUserId,
            model.Title,
            model.Details,
            model.DueAtUtc,
            model.Status.ToString(),
            model.IsOverdue,
            model.CreatedAtUtc,
            model.StatusChangedAtUtc);

    public static RecommendationTemplateHttpResponse ToHttpResponse(this RecommendationTemplateModel model) =>
        new(
            model.Id,
            model.Name,
            model.Text,
            model.IsArchived,
            model.CreatedAtUtc,
            model.ModifiedAtUtc);

    public static BulkRecommendationResultHttpResponse ToHttpResponse(this BulkRecommendationResultModel model) =>
        new(
            model.IdempotencyKey,
            model.Recipients.Select(recipient => new BulkRecommendationRecipientResultHttpResponse(
                recipient.ClientUserId,
                recipient.Succeeded,
                recipient.RecommendationId,
                recipient.WasAlreadyProcessed,
                recipient.ErrorCode)).ToList());
}
