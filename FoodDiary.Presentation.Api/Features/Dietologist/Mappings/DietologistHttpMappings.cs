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
    public static GetAttentionSignalsQuery ToQuery(this GetAttentionSignalsHttpQuery query, Guid userId) =>
        new(
            userId,
            query.InactivityDays,
            query.CalorieDeviationPercent,
            query.SustainedDays,
            query.WeightChangePercent,
            query.LookbackDays);

    public static SetAttentionSignalStateCommand ToCommand(
        this SetAttentionSignalStateHttpRequest request,
        Guid userId,
        string signalId) =>
        new(userId, request.ClientUserId, signalId, request.Action, request.SnoozedUntilUtc);

    public static InviteDietologistCommand ToCommand(this InviteDietologistHttpRequest request, Guid userId) =>
        new(userId, request.DietologistEmail, request.Permissions.ToInput());

    public static AcceptInvitationCommand ToCommand(this AcceptInvitationHttpRequest request, Guid userId) =>
        new(request.InvitationId, request.Token, userId);

    public static AcceptInvitationForCurrentUserCommand ToCurrentUserAcceptCommand(this Guid invitationId, Guid userId) =>
        new(userId, invitationId);

    public static DeclineInvitationCommand ToCommand(this DeclineInvitationHttpRequest request, Guid userId) =>
        new(request.InvitationId, request.Token, userId);

    public static DeclineInvitationForCurrentUserCommand ToCurrentUserDeclineCommand(this Guid invitationId, Guid userId) =>
        new(userId, invitationId);

    public static UpdateDietologistPermissionsCommand ToCommand(this UpdateDietologistPermissionsHttpRequest request, Guid userId) =>
        new(userId, request.Permissions.ToInput());

    public static RevokeInvitationCommand ToRevokeInvitationCommand(this Guid userId) => new(userId);

    public static DisconnectDietologistCommand ToCommand(this DisconnectClientHttpRequest request, Guid userId) =>
        new(userId, request.ClientUserId);

    public static GetMyDietologistQuery ToMyDietologistQuery(this Guid userId) => new(userId);

    public static GetMyDietologistRelationshipQuery ToMyDietologistRelationshipQuery(this Guid userId) => new(userId);

    public static GetMyClientsQuery ToMyClientsQuery(this Guid userId) => new(userId);

    public static GetInvitationByTokenQuery ToInvitationQuery(this Guid invitationId, Guid userId) => new(userId, invitationId);

    public static GetInvitationForCurrentUserQuery ToCurrentUserInvitationQuery(this Guid invitationId, Guid userId) =>
        new(userId, invitationId);

    public static GetClientDashboardQuery ToClientDashboardQuery(
        this GetClientDashboardHttpQuery query, Guid userId, Guid clientUserId, DateTime todayUtc) {
        DateTime dateFrom = query.DateFrom ?? query.Date ?? todayUtc.Date;
        DateTime? dateTo = query.DateTo ?? query.Date;

        return new(userId, clientUserId, dateFrom, dateTo, query.Page, query.PageSize, query.Locale, query.TrendDays);
    }

    public static GetClientGoalsQuery ToClientGoalsQuery(this Guid clientUserId, Guid userId) =>
        new(userId, clientUserId);

    public static CreateRecommendationCommand ToCommand(
        this CreateRecommendationHttpRequest request, Guid userId, Guid clientUserId) =>
        new(userId, clientUserId, request.Text);

    public static CreateClientTaskCommand ToCommand(
        this CreateClientTaskHttpRequest request,
        Guid userId,
        Guid clientUserId) =>
        new(userId, clientUserId, request.Title, request.Details, request.DueAtUtc);

    public static GetClientTasksForDietologistQuery ToClientTasksQuery(this Guid clientUserId, Guid userId) =>
        new(userId, clientUserId);

    public static GetMyClientTasksQuery ToMyClientTasksQuery(this Guid userId) => new(userId);

    public static ChangeClientTaskStatusCommand ToCommand(
        this ChangeClientTaskStatusHttpRequest request,
        Guid userId,
        Guid taskId) =>
        new(userId, taskId, request.Status);

    public static CancelClientTaskCommand ToCancelClientTaskCommand(this Guid taskId, Guid userId) =>
        new(userId, taskId);

    public static CreateRecommendationTemplateCommand ToCreateTemplateCommand(
        this RecommendationTemplateHttpRequest request,
        Guid userId) =>
        new(userId, request.Name, request.Text);

    public static UpdateRecommendationTemplateCommand ToUpdateTemplateCommand(
        this RecommendationTemplateHttpRequest request,
        Guid templateId,
        Guid userId) =>
        new(userId, templateId, request.Name, request.Text);

    public static ArchiveRecommendationTemplateCommand ToArchiveTemplateCommand(this Guid templateId, Guid userId) =>
        new(userId, templateId);

    public static SearchRecommendationTemplatesQuery ToSearchTemplatesQuery(
        this Guid userId,
        string? search,
        bool includeArchived) =>
        new(userId, search, includeArchived);

    public static BulkCreateRecommendationsCommand ToCommand(
        this BulkCreateRecommendationsHttpRequest request,
        Guid userId) =>
        new(userId, request.ClientUserIds, request.Text, request.IdempotencyKey);

    public static CreateRecommendationCommentCommand ToCommand(
        this CreateRecommendationCommentHttpRequest request,
        Guid userId,
        Guid recommendationId) =>
        new(userId, recommendationId, request.Text);

    public static GetRecommendationCommentsQuery ToRecommendationCommentsQuery(
        this Guid recommendationId,
        Guid userId) =>
        new(userId, recommendationId);

    public static GetRecommendationsForClientQuery ToRecommendationsForClientQuery(
        this Guid clientUserId, Guid userId) =>
        new(userId, clientUserId);

    public static GetMyRecommendationsQuery ToMyRecommendationsQuery(this Guid userId) => new(userId);

    public static MarkRecommendationReadCommand ToMarkReadCommand(this Guid recommendationId, Guid userId) =>
        new(userId, recommendationId);

    private static DietologistPermissionsInput ToInput(this DietologistPermissionsHttpRequest request) =>
        new(request.ShareMeals, request.ShareStatistics, request.ShareWeight,
            request.ShareWaist, request.ShareGoals, request.ShareHydration, request.ShareProfile, request.ShareFasting);
}
