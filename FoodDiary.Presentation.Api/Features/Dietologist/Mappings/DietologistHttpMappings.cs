using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;
using FoodDiary.Application.Dietologist.Commands.InviteDietologist;
using FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;
using FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetMyRecommendations;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;
using FoodDiary.Application.Dietologist.Queries.GetClientDashboard;
using FoodDiary.Application.Dietologist.Queries.GetClientGoals;
using FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;
using FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;
using FoodDiary.Application.Dietologist.Queries.GetMyClients;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologist;
using FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Mappings;

public static class DietologistHttpMappings {
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

    public static DisconnectDietologistCommand ToCommand(this DisconnectClientHttpRequest request, Guid userId) =>
        new(userId, request.ClientUserId);

    public static GetMyDietologistQuery ToMyDietologistQuery(this Guid userId) => new(userId);

    public static GetMyDietologistRelationshipQuery ToMyDietologistRelationshipQuery(this Guid userId) => new(userId);

    public static GetMyClientsQuery ToMyClientsQuery(this Guid userId) => new(userId);

    public static GetInvitationByTokenQuery ToInvitationQuery(this Guid invitationId) => new(invitationId);

    public static GetInvitationForCurrentUserQuery ToCurrentUserInvitationQuery(this Guid invitationId, Guid userId) =>
        new(userId, invitationId);

    public static GetClientDashboardQuery ToClientDashboardQuery(
        this GetClientDashboardHttpQuery query, Guid userId, Guid clientUserId) =>
        new(userId, clientUserId, query.Date, query.Page, query.PageSize, query.Locale, query.TrendDays);

    public static GetClientGoalsQuery ToClientGoalsQuery(this Guid clientUserId, Guid userId) =>
        new(userId, clientUserId);

    public static CreateRecommendationCommand ToCommand(
        this CreateRecommendationHttpRequest request, Guid userId, Guid clientUserId) =>
        new(userId, clientUserId, request.Text);

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
