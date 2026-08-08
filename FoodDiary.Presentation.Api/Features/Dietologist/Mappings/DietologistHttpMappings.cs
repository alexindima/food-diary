using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.CancelClientTask;
using FoodDiary.Application.Dietologist.Commands.ArchiveRecommendationTemplate;
using FoodDiary.Application.Dietologist.Commands.BulkCreateRecommendations;
using FoodDiary.Application.Dietologist.Commands.ChangeClientTaskStatus;
using FoodDiary.Application.Dietologist.Commands.CreateClientTask;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendationTemplate;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendation;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendationComment;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;
using FoodDiary.Application.Dietologist.Commands.InviteDietologist;
using FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;
using FoodDiary.Application.Dietologist.Commands.RevokeInvitation;
using FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;
using FoodDiary.Application.Dietologist.Commands.UpdateRecommendationTemplate;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetMyRecommendations;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationComments;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;
using FoodDiary.Application.Dietologist.Queries.GetClientDashboard;
using FoodDiary.Application.Dietologist.Queries.GetAttentionSignals;
using FoodDiary.Application.Dietologist.Commands.SetAttentionSignalState;
using FoodDiary.Application.Dietologist.Queries.GetClientGoals;
using FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;
using FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Queries.GetMyClients;
using FoodDiary.Application.Dietologist.Queries.GetMyClientTasks;
using FoodDiary.Application.Dietologist.Queries.GetClientTasksForDietologist;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologist;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;
using FoodDiary.Application.Dietologist.Queries.SearchRecommendationTemplates;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Mappings;

public static class DietologistHttpMappings {
    extension(GetAttentionSignalsHttpQuery query) {
        public GetAttentionSignalsQuery ToQuery(Guid userId) =>
                new(
                    userId,
                    query.InactivityDays,
                    query.CalorieDeviationPercent,
                    query.SustainedDays,
                    query.WeightChangePercent,
                    query.LookbackDays);
    }

    extension(SetAttentionSignalStateHttpRequest request) {
        public SetAttentionSignalStateCommand ToCommand(
                Guid userId,
                string signalId) =>
                new(userId, request.ClientUserId, signalId, request.Action, request.SnoozedUntilUtc);
    }

    extension(InviteDietologistHttpRequest request) {
        public InviteDietologistCommand ToCommand(Guid userId) =>
                new(userId, request.DietologistEmail, request.Permissions.ToInput());
    }

    extension(AcceptInvitationHttpRequest request) {
        public AcceptInvitationCommand ToCommand(Guid userId) =>
                new(request.InvitationId, request.Token, userId);
    }

    extension(Guid invitationId) {
        public AcceptInvitationForCurrentUserCommand ToCurrentUserAcceptCommand(Guid userId) =>
                new(userId, invitationId);

        public DeclineInvitationForCurrentUserCommand ToCurrentUserDeclineCommand(Guid userId) =>
                new(userId, invitationId);

        public GetInvitationByTokenQuery ToInvitationQuery(Guid userId) => new(userId, invitationId);

        public GetInvitationForCurrentUserQuery ToCurrentUserInvitationQuery(Guid userId) =>
                new(userId, invitationId);
    }

    extension(DeclineInvitationHttpRequest request) {
        public DeclineInvitationCommand ToCommand(Guid userId) =>
                new(request.InvitationId, request.Token, userId);
    }

    extension(UpdateDietologistPermissionsHttpRequest request) {
        public UpdateDietologistPermissionsCommand ToCommand(Guid userId) =>
                new(userId, request.Permissions.ToInput());
    }

    extension(Guid userId) {
        public RevokeInvitationCommand ToRevokeInvitationCommand() => new(userId);

        public GetMyDietologistQuery ToMyDietologistQuery() => new(userId);

        public GetMyDietologistRelationshipQuery ToMyDietologistRelationshipQuery() => new(userId);

        public GetMyClientsQuery ToMyClientsQuery() => new(userId);

        public GetMyClientTasksQuery ToMyClientTasksQuery() => new(userId);

        public SearchRecommendationTemplatesQuery ToSearchTemplatesQuery(
                string? search,
                bool includeArchived) =>
                new(userId, search, includeArchived);

        public GetMyRecommendationsQuery ToMyRecommendationsQuery() => new(userId);
    }

    extension(DisconnectClientHttpRequest request) {
        public DisconnectDietologistCommand ToCommand(Guid userId) =>
                new(userId, request.ClientUserId);
    }

    extension(GetClientDashboardHttpQuery query) {
        public GetClientDashboardQuery ToClientDashboardQuery(
        Guid userId, Guid clientUserId, DateTime todayUtc) {
            DateTime dateFrom = query.DateFrom ?? query.Date ?? todayUtc.Date;
            DateTime? dateTo = query.DateTo ?? query.Date;

            return new(userId, clientUserId, dateFrom, dateTo, query.Page, query.PageSize, query.Locale, query.TrendDays);
        }
    }

    extension(Guid clientUserId) {
        public GetClientGoalsQuery ToClientGoalsQuery(Guid userId) =>
                new(userId, clientUserId);

        public GetClientTasksForDietologistQuery ToClientTasksQuery(Guid userId) =>
                new(userId, clientUserId);

        public GetRecommendationsForClientQuery ToRecommendationsForClientQuery(
        Guid userId) =>
                new(userId, clientUserId);
    }

    extension(CreateRecommendationHttpRequest request) {
        public CreateRecommendationCommand ToCommand(
        Guid userId, Guid clientUserId) =>
                new(userId, clientUserId, request.Text);
    }

    extension(CreateClientTaskHttpRequest request) {
        public CreateClientTaskCommand ToCommand(
                Guid userId,
                Guid clientUserId) =>
                new(userId, clientUserId, request.Title, request.Details, request.DueAtUtc);
    }

    extension(ChangeClientTaskStatusHttpRequest request) {
        public ChangeClientTaskStatusCommand ToCommand(
                Guid userId,
                Guid taskId) =>
                new(userId, taskId, request.Status);
    }

    extension(Guid taskId) {
        public CancelClientTaskCommand ToCancelClientTaskCommand(Guid userId) =>
                new(userId, taskId);
    }

    extension(RecommendationTemplateHttpRequest request) {
        public CreateRecommendationTemplateCommand ToCreateTemplateCommand(
                Guid userId) =>
                new(userId, request.Name, request.Text);

        public UpdateRecommendationTemplateCommand ToUpdateTemplateCommand(
                Guid templateId,
                Guid userId) =>
                new(userId, templateId, request.Name, request.Text);
    }

    extension(Guid templateId) {
        public ArchiveRecommendationTemplateCommand ToArchiveTemplateCommand(Guid userId) =>
                new(userId, templateId);
    }

    extension(BulkCreateRecommendationsHttpRequest request) {
        public BulkCreateRecommendationsCommand ToCommand(
                Guid userId) =>
                new(userId, request.ClientUserIds, request.Text, request.IdempotencyKey);
    }

    extension(CreateRecommendationCommentHttpRequest request) {
        public CreateRecommendationCommentCommand ToCommand(
                Guid userId,
                Guid recommendationId) =>
                new(userId, recommendationId, request.Text);
    }

    extension(Guid recommendationId) {
        public GetRecommendationCommentsQuery ToRecommendationCommentsQuery(
                Guid userId) =>
                new(userId, recommendationId);

        public MarkRecommendationReadCommand ToMarkReadCommand(Guid userId) =>
                new(userId, recommendationId);
    }

    extension(DietologistPermissionsHttpRequest request) {
        private DietologistPermissionsInput ToInput() =>
                new(request.ShareMeals, request.ShareStatistics, request.ShareWeight,
                    request.ShareWaist, request.ShareGoals, request.ShareHydration, request.ShareProfile, request.ShareFasting);
    }
}
