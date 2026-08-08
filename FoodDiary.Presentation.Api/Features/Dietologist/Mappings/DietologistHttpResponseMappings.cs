using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Mappings;

public static class DietologistHttpResponseMappings {
    extension(AttentionSignalModel model) {
        public AttentionSignalHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.ClientUserId,
                    model.ClientDisplayName,
                    model.Type,
                    model.Severity,
                    model.Reason,
                    model.DetectedAtUtc,
                    model.SnoozedUntilUtc);
    }

    extension(DietologistInvitationForCurrentUserModel model) {
        public DietologistInvitationForCurrentUserHttpResponse ToHttpResponse() =>
                new(
                    model.InvitationId,
                    model.ClientUserId,
                    model.ClientEmail,
                    model.ClientFirstName,
                    model.ClientLastName,
                    model.Status,
                    model.CreatedAtUtc,
                    model.ExpiresAtUtc);
    }

    extension(DietologistRelationshipModel model) {
        public DietologistRelationshipHttpResponse ToHttpResponse() =>
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
    }

    extension(ProfileDietologistRelationshipModel model) {
        public DietologistRelationshipHttpResponse ToHttpResponse() =>
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
    }

    extension(DietologistInfoModel model) {
        public DietologistInfoHttpResponse ToHttpResponse() =>
                new(model.InvitationId, model.DietologistUserId, model.Email,
                    model.FirstName, model.LastName,
                    model.Permissions.ToHttpResponse(), model.AcceptedAtUtc);
    }

    extension(ClientSummaryModel model) {
        public ClientSummaryHttpResponse ToHttpResponse() =>
                new(model.UserId, model.Email, model.FirstName, model.LastName,
                    model.ProfileImage, model.BirthDate, model.Gender, model.Height, model.ActivityLevel,
                    model.Permissions.ToHttpResponse(), model.AcceptedAtUtc);
    }

    extension(InvitationModel model) {
        public InvitationHttpResponse ToHttpResponse() =>
                new(model.InvitationId, model.ClientEmail, model.ClientFirstName,
                    model.ClientLastName, model.Status, model.CreatedAtUtc, model.ExpiresAtUtc);
    }

    extension(DietologistPermissionsModel permissions) {
        public DietologistPermissionsHttpResponse ToHttpResponse() =>
                new(permissions.ShareMeals, permissions.ShareStatistics, permissions.ShareWeight,
                    permissions.ShareWaist, permissions.ShareGoals, permissions.ShareHydration, permissions.ShareProfile, permissions.ShareFasting);
    }

    extension(RecommendationModel model) {
        public RecommendationHttpResponse ToHttpResponse() =>
                new(model.Id, model.DietologistUserId, model.DietologistFirstName,
                    model.DietologistLastName, model.Text, model.IsRead,
                    model.CreatedAtUtc, model.ReadAtUtc);
    }

    extension(RecommendationCommentModel model) {
        public RecommendationCommentHttpResponse ToHttpResponse() =>
                new(model.Id, model.RecommendationId, model.AuthorUserId,
                    model.AuthorFirstName, model.AuthorLastName, model.AuthorEmail,
                    model.Text, model.CreatedAtUtc);
    }

    extension(ClientTaskModel model) {
        public ClientTaskHttpResponse ToHttpResponse() =>
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
    }

    extension(RecommendationTemplateModel model) {
        public RecommendationTemplateHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.Name,
                    model.Text,
                    model.IsArchived,
                    model.CreatedAtUtc,
                    model.ModifiedAtUtc);
    }

    extension(BulkRecommendationResultModel model) {
        public BulkRecommendationResultHttpResponse ToHttpResponse() =>
                new(
                    model.IdempotencyKey,
                    model.Recipients.Select(recipient => new BulkRecommendationRecipientResultHttpResponse(
                        recipient.ClientUserId,
                        recipient.Succeeded,
                        recipient.RecommendationId,
                        recipient.WasAlreadyProcessed,
                        recipient.ErrorCode)).ToList());
    }
}
