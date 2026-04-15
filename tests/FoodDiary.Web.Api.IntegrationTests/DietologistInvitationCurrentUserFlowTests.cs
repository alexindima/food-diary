using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class DietologistInvitationCurrentUserFlowTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Invite_ThenAcceptCurrentUser_ThenGetCurrentUserInvitation_ReturnsAcceptedStatus() {
        var clientUser = await CreateAuthenticatedClientAsync();
        var dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        var inviteResponse = await clientUser.Client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(
                dietologistUser.Email,
                new DietologistPermissionsHttpRequest(
                    ShareMeals: true,
                    ShareStatistics: true,
                    ShareWeight: true,
                    ShareWaist: false,
                    ShareGoals: true,
                    ShareHydration: true)));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse);

        var relationshipResponse = await clientUser.Client.GetAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.OK, relationshipResponse);
        var relationship = await relationshipResponse.Content.ReadFromJsonAsync<DietologistRelationshipPayload>(JsonOptions);

        Assert.NotNull(relationship);
        Assert.Equal("Pending", relationship.Status);
        Assert.Equal(dietologistUser.Email, relationship.Email);

        var invitationResponse = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/invitations/{relationship.InvitationId}/current-user");
        await AssertStatusCodeAsync(HttpStatusCode.OK, invitationResponse);
        var invitation = await invitationResponse.Content.ReadFromJsonAsync<DietologistInvitationForCurrentUserPayload>(JsonOptions);

        Assert.NotNull(invitation);
        Assert.Equal("Pending", invitation.Status);

        var acceptResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/accept-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, acceptResponse);

        var acceptedResponse = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/invitations/{relationship.InvitationId}/current-user");
        await AssertStatusCodeAsync(HttpStatusCode.OK, acceptedResponse);
        var acceptedInvitation = await acceptedResponse.Content.ReadFromJsonAsync<DietologistInvitationForCurrentUserPayload>(JsonOptions);

        Assert.NotNull(acceptedInvitation);
        Assert.Equal("Accepted", acceptedInvitation.Status);
        Assert.Equal(clientUser.Email, acceptedInvitation.ClientEmail);

        var updatedRelationshipResponse = await clientUser.Client.GetAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.OK, updatedRelationshipResponse);
        var updatedRelationship = await updatedRelationshipResponse.Content.ReadFromJsonAsync<DietologistRelationshipPayload>(JsonOptions);

        Assert.NotNull(updatedRelationship);
        Assert.Equal("Accepted", updatedRelationship.Status);
    }

    [Fact]
    public async Task Invite_ThenDeclineCurrentUser_ThenGetCurrentUserInvitation_ReturnsDeclinedStatus() {
        var clientUser = await CreateAuthenticatedClientAsync();
        var dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        var inviteResponse = await clientUser.Client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(
                dietologistUser.Email,
                new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse);

        var relationshipResponse = await clientUser.Client.GetAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.OK, relationshipResponse);
        var relationship = await relationshipResponse.Content.ReadFromJsonAsync<DietologistRelationshipPayload>(JsonOptions);

        Assert.NotNull(relationship);

        var declineResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/decline-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, declineResponse);

        var invitationResponse = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/invitations/{relationship.InvitationId}/current-user");
        await AssertStatusCodeAsync(HttpStatusCode.OK, invitationResponse);
        var invitation = await invitationResponse.Content.ReadFromJsonAsync<DietologistInvitationForCurrentUserPayload>(JsonOptions);

        Assert.NotNull(invitation);
        Assert.Equal("Declined", invitation.Status);
    }

    private async Task<AuthenticatedUser> CreateAuthenticatedClientAsync(string emailPrefix = "api-tests") {
        var client = factory.CreateClient();
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        registerResponse.EnsureSuccessStatusCode();

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(authPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        return new AuthenticatedUser(client, email);
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"Expected {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private sealed record AuthPayload(string AccessToken);

    private sealed record AuthenticatedUser(HttpClient Client, string Email);

    private sealed record DietologistRelationshipPayload(
        Guid InvitationId,
        string Status,
        string Email,
        string? FirstName,
        string? LastName,
        Guid? DietologistUserId,
        object Permissions,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        DateTime? AcceptedAtUtc);

    private sealed record DietologistInvitationForCurrentUserPayload(
        Guid InvitationId,
        Guid ClientUserId,
        string ClientEmail,
        string? ClientFirstName,
        string? ClientLastName,
        string Status,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc);
}
