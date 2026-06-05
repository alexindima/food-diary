using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistHttpMappingsTests {
    [Fact]
    public void InviteDietologistRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new InviteDietologistHttpRequest(
            "diet@example.com",
            new DietologistPermissionsHttpRequest(true, false, true, false, true, false, true, false));

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal("diet@example.com", command.DietologistEmail);
        Assert.True(command.Permissions.ShareMeals);
        Assert.False(command.Permissions.ShareStatistics);
        Assert.True(command.Permissions.ShareWeight);
        Assert.False(command.Permissions.ShareWaist);
        Assert.True(command.Permissions.ShareGoals);
        Assert.False(command.Permissions.ShareHydration);
        Assert.True(command.Permissions.ShareProfile);
        Assert.False(command.Permissions.ShareFasting);
    }

    [Fact]
    public void AcceptInvitationRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var request = new AcceptInvitationHttpRequest(invitationId, "token-value");

        var command = request.ToCommand(userId);

        Assert.Equal(invitationId, command.InvitationId);
        Assert.Equal("token-value", command.Token);
        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public void CreateRecommendationRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var request = new CreateRecommendationHttpRequest("Eat more protein");

        var command = request.ToCommand(userId, clientUserId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(clientUserId, command.ClientUserId);
        Assert.Equal("Eat more protein", command.Text);
    }

    [Fact]
    public void GetClientDashboardQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 1);
        var httpQuery = new GetClientDashboardHttpQuery(date, Page: 2, PageSize: 20, Locale: "ru", TrendDays: 14);

        var query = httpQuery.ToClientDashboardQuery(userId, clientUserId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(clientUserId, query.ClientUserId);
        Assert.Equal(date, query.Date);
        Assert.Equal(2, query.Page);
        Assert.Equal(20, query.PageSize);
        Assert.Equal("ru", query.Locale);
        Assert.Equal(14, query.TrendDays);
    }

    [Fact]
    public void GetClientDashboardQuery_UsesDateRangeWhenProvided() {
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 1);
        var dateFrom = new DateTime(2026, 4, 2);
        var dateTo = new DateTime(2026, 4, 7);
        var httpQuery = new GetClientDashboardHttpQuery(
            date,
            DateFrom: dateFrom,
            DateTo: dateTo,
            Page: 1,
            PageSize: 10,
            Locale: "en",
            TrendDays: 7);

        var query = httpQuery.ToClientDashboardQuery(userId, clientUserId);

        Assert.Equal(dateFrom, query.Date);
        Assert.Equal(dateTo, query.DateTo);
    }

    [Fact]
    public void DietologistIds_ToQueriesAndCommands_MapIds() {
        var userId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();

        Assert.Equal(userId, userId.ToMyDietologistQuery().UserId);
        Assert.Equal(userId, userId.ToMyDietologistRelationshipQuery().UserId);
        Assert.Equal(userId, userId.ToMyClientsQuery().UserId);
        Assert.Equal(userId, userId.ToMyRecommendationsQuery().UserId);

        var invitationQuery = invitationId.ToInvitationQuery(userId);
        Assert.Equal(userId, invitationQuery.UserId);
        Assert.Equal(invitationId, invitationQuery.InvitationId);

        var currentInvitationQuery = invitationId.ToCurrentUserInvitationQuery(userId);
        Assert.Equal(userId, currentInvitationQuery.UserId);
        Assert.Equal(invitationId, currentInvitationQuery.InvitationId);

        var goalsQuery = clientUserId.ToClientGoalsQuery(userId);
        Assert.Equal(userId, goalsQuery.UserId);
        Assert.Equal(clientUserId, goalsQuery.ClientUserId);

        var recommendationsQuery = clientUserId.ToRecommendationsForClientQuery(userId);
        Assert.Equal(userId, recommendationsQuery.UserId);
        Assert.Equal(clientUserId, recommendationsQuery.ClientUserId);

        var markRead = recommendationId.ToMarkReadCommand(userId);
        Assert.Equal(userId, markRead.UserId);
        Assert.Equal(recommendationId, markRead.RecommendationId);

        var acceptCurrent = invitationId.ToCurrentUserAcceptCommand(userId);
        Assert.Equal(userId, acceptCurrent.UserId);
        Assert.Equal(invitationId, acceptCurrent.InvitationId);

        var declineCurrent = invitationId.ToCurrentUserDeclineCommand(userId);
        Assert.Equal(userId, declineCurrent.UserId);
        Assert.Equal(invitationId, declineCurrent.InvitationId);
    }

    [Fact]
    public void DietologistRequests_ToCommands_MapAllFields() {
        var userId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var permissions = new DietologistPermissionsHttpRequest(
            ShareMeals: false,
            ShareStatistics: true,
            ShareWeight: false,
            ShareWaist: true,
            ShareGoals: false,
            ShareHydration: true,
            ShareProfile: false,
            ShareFasting: true);

        var decline = new DeclineInvitationHttpRequest(invitationId, "decline-token").ToCommand(userId);
        var updatePermissions = new UpdateDietologistPermissionsHttpRequest(permissions).ToCommand(userId);
        var disconnect = new DisconnectClientHttpRequest(clientUserId).ToCommand(userId);

        Assert.Equal(invitationId, decline.InvitationId);
        Assert.Equal("decline-token", decline.Token);
        Assert.Equal(userId, decline.UserId);
        Assert.Equal(userId, updatePermissions.UserId);
        Assert.True(updatePermissions.Permissions.ShareStatistics);
        Assert.True(updatePermissions.Permissions.ShareWaist);
        Assert.True(updatePermissions.Permissions.ShareHydration);
        Assert.True(updatePermissions.Permissions.ShareFasting);
        Assert.Equal(userId, disconnect.UserId);
        Assert.Equal(clientUserId, disconnect.ClientUserId);
    }

    [Fact]
    public void DietologistInvitationForCurrentUserModel_ToHttpResponse_MapsAllFields() {
        var invitationId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow.AddDays(-1);
        var expiresAtUtc = DateTime.UtcNow.AddDays(5);
        var model = new DietologistInvitationForCurrentUserModel(
            invitationId,
            clientUserId,
            "client@example.com",
            "Client",
            "User",
            "Pending",
            createdAtUtc,
            expiresAtUtc);

        var response = model.ToHttpResponse();

        Assert.Equal(invitationId, response.InvitationId);
        Assert.Equal(clientUserId, response.ClientUserId);
        Assert.Equal("client@example.com", response.ClientEmail);
        Assert.Equal("Client", response.ClientFirstName);
        Assert.Equal("User", response.ClientLastName);
        Assert.Equal("Pending", response.Status);
        Assert.Equal(createdAtUtc, response.CreatedAtUtc);
        Assert.Equal(expiresAtUtc, response.ExpiresAtUtc);
    }

    [Fact]
    public void DietologistResponseMappings_MapRelationshipInfoClientInvitationRecommendation() {
        var invitationId = Guid.NewGuid();
        var dietologistUserId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var recommendationId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow.AddDays(-2);
        var acceptedAtUtc = DateTime.UtcNow.AddDays(-1);
        var expiresAtUtc = DateTime.UtcNow.AddDays(5);
        var permissions = new DietologistPermissionsModel(
            ShareMeals: true,
            ShareStatistics: false,
            ShareWeight: true,
            ShareWaist: false,
            ShareGoals: true,
            ShareHydration: false,
            ShareProfile: true,
            ShareFasting: false);

        var relationship = new DietologistRelationshipModel(
            invitationId,
            Status: "Accepted",
            Email: "diet@example.com",
            FirstName: "Dana",
            LastName: "Doc",
            dietologistUserId,
            permissions,
            createdAtUtc,
            expiresAtUtc,
            acceptedAtUtc).ToHttpResponse();
        var info = new DietologistInfoModel(
            invitationId,
            dietologistUserId,
            "diet@example.com",
            "Dana",
            "Doc",
            permissions,
            acceptedAtUtc).ToHttpResponse();
        var client = new ClientSummaryModel(
            clientUserId,
            "client@example.com",
            "Client",
            "User",
            "https://cdn.example/profile.png",
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "Female",
            170,
            "Moderate",
            permissions,
            acceptedAtUtc).ToHttpResponse();
        var invitation = new InvitationModel(
            invitationId,
            "client@example.com",
            "Client",
            "User",
            "Pending",
            createdAtUtc,
            expiresAtUtc).ToHttpResponse();
        var recommendation = new RecommendationModel(
            recommendationId,
            dietologistUserId,
            "Dana",
            "Doc",
            "Eat more fiber",
            IsRead: true,
            createdAtUtc,
            acceptedAtUtc).ToHttpResponse();

        Assert.Equal(invitationId, relationship.InvitationId);
        Assert.True(relationship.Permissions.ShareMeals);
        Assert.Equal(dietologistUserId, info.DietologistUserId);
        Assert.Equal(clientUserId, client.UserId);
        Assert.Equal("Pending", invitation.Status);
        Assert.Equal(recommendationId, recommendation.Id);
        Assert.Equal("Eat more fiber", recommendation.Text);
        Assert.True(recommendation.IsRead);
    }
}
