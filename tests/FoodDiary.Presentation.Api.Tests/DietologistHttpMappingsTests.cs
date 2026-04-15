using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;

namespace FoodDiary.Presentation.Api.Tests;

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
}
