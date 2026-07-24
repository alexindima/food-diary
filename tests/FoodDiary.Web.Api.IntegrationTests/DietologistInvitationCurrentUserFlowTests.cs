using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class DietologistInvitationCurrentUserFlowTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Invite_ThenAcceptCurrentUser_ThenGetCurrentUserInvitation_ReturnsAcceptedStatus() {
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync();
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        HttpResponseMessage inviteResponse = await clientUser.Client.PostAsJsonAsync(
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

        HttpResponseMessage relationshipResponse = await clientUser.Client.GetAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.OK, relationshipResponse);
        DietologistRelationshipPayload? relationship = await relationshipResponse.Content.ReadFromJsonAsync<DietologistRelationshipPayload>(JsonOptions);

        Assert.NotNull(relationship);
        Assert.Equal("Pending", relationship.Status);
        Assert.Equal(dietologistUser.Email, relationship.Email);

        HttpResponseMessage invitationResponse = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/invitations/{relationship.InvitationId}/current-user");
        await AssertStatusCodeAsync(HttpStatusCode.OK, invitationResponse);
        DietologistInvitationForCurrentUserPayload? invitation = await invitationResponse.Content.ReadFromJsonAsync<DietologistInvitationForCurrentUserPayload>(JsonOptions);

        Assert.NotNull(invitation);
        Assert.Equal("Pending", invitation.Status);

        HttpResponseMessage acceptResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/accept-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, acceptResponse);

        HttpResponseMessage acceptedResponse = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/invitations/{relationship.InvitationId}/current-user");
        await AssertStatusCodeAsync(HttpStatusCode.OK, acceptedResponse);
        DietologistInvitationForCurrentUserPayload? acceptedInvitation = await acceptedResponse.Content.ReadFromJsonAsync<DietologistInvitationForCurrentUserPayload>(JsonOptions);

        Assert.NotNull(acceptedInvitation);
        Assert.Equal("Accepted", acceptedInvitation.Status);
        Assert.Equal(clientUser.Email, acceptedInvitation.ClientEmail);

        HttpResponseMessage updatedRelationshipResponse = await clientUser.Client.GetAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.OK, updatedRelationshipResponse);
        DietologistRelationshipPayload? updatedRelationship = await updatedRelationshipResponse.Content.ReadFromJsonAsync<DietologistRelationshipPayload>(JsonOptions);

        Assert.NotNull(updatedRelationship);
        Assert.Equal("Accepted", updatedRelationship.Status);
    }

    [Fact]
    public async Task Invite_ThenDeclineCurrentUser_ThenGetCurrentUserInvitation_ReturnsDeclinedStatus() {
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync();
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        HttpResponseMessage inviteResponse = await clientUser.Client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(
                dietologistUser.Email,
                new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse);

        HttpResponseMessage relationshipResponse = await clientUser.Client.GetAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.OK, relationshipResponse);
        DietologistRelationshipPayload? relationship = await relationshipResponse.Content.ReadFromJsonAsync<DietologistRelationshipPayload>(JsonOptions);

        Assert.NotNull(relationship);

        HttpResponseMessage declineResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/decline-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, declineResponse);

        HttpResponseMessage invitationResponse = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/invitations/{relationship.InvitationId}/current-user");
        await AssertStatusCodeAsync(HttpStatusCode.OK, invitationResponse);
        DietologistInvitationForCurrentUserPayload? invitation = await invitationResponse.Content.ReadFromJsonAsync<DietologistInvitationForCurrentUserPayload>(JsonOptions);

        Assert.NotNull(invitation);
        Assert.Equal("Declined", invitation.Status);
    }

    [Fact]
    public async Task PermissionsAndDisconnect_TakeEffectOnNextDietologistApiRequest() {
        await EnsureDietologistRoleAsync();
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync();
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");
        SetDietologistToken(dietologistUser);

        HttpResponseMessage inviteResponse = await clientUser.Client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(dietologistUser.Email, new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse);
        DietologistRelationshipPayload relationship = await GetRelationshipAsync(clientUser.Client);
        HttpResponseMessage acceptResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/accept-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, acceptResponse);

        string dashboardUrl = string.Create(
            CultureInfo.InvariantCulture,
            $"/api/v1/dietologist/clients/{clientUser.UserId}/dashboard?dateFrom=2026-07-01&dateTo=2026-07-14&page=1&pageSize=10&trendDays=14&locale=en");
        await AssertStatusCodeAsync(HttpStatusCode.OK, await dietologistUser.Client.GetAsync(dashboardUrl));

        var noPermissions = new DietologistPermissionsHttpRequest(
            ShareMeals: false,
            ShareStatistics: false,
            ShareWeight: false,
            ShareWaist: false,
            ShareGoals: false,
            ShareHydration: false,
            ShareProfile: false,
            ShareFasting: false);
        HttpResponseMessage updateResponse = await clientUser.Client.PutAsJsonAsync(
            "/api/v1/dietologist/permissions",
            new UpdateDietologistPermissionsHttpRequest(noPermissions));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, updateResponse);

        HttpResponseMessage revokedPermissionResponse = await dietologistUser.Client.GetAsync(dashboardUrl);
        Assert.Equal(HttpStatusCode.Forbidden, revokedPermissionResponse.StatusCode);

        HttpResponseMessage restoreResponse = await clientUser.Client.PutAsJsonAsync(
            "/api/v1/dietologist/permissions",
            new UpdateDietologistPermissionsHttpRequest(new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, restoreResponse);
        await AssertStatusCodeAsync(HttpStatusCode.OK, await dietologistUser.Client.GetAsync(dashboardUrl));

        HttpResponseMessage disconnectResponse = await clientUser.Client.DeleteAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, disconnectResponse);
        HttpResponseMessage disconnectedResponse = await dietologistUser.Client.GetAsync(dashboardUrl);
        Assert.Equal(HttpStatusCode.Forbidden, disconnectedResponse.StatusCode);
    }

    private async Task<AuthenticatedUser> CreateAuthenticatedClientAsync(string emailPrefix = "api-tests") {
        HttpClient client = factory.CreateClient();
        string email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en")).ConfigureAwait(false);
        registerResponse.EnsureSuccessStatusCode();

        AuthPayload? authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(authPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        Assert.NotNull(authPayload.User);
        return new AuthenticatedUser(client, email, authPayload.User.Id);
    }

    private async Task EnsureDietologistRoleAsync() {
        AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            if (!dbContext.Roles.Any(role => role.Name == RoleNames.Dietologist)) {
                dbContext.Roles.Add(Role.Create(RoleNames.Dietologist));
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }

    private void SetDietologistToken(AuthenticatedUser user) {
        using IServiceScope scope = factory.Services.CreateScope();
        IJwtTokenGenerator tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        string token = tokenGenerator.GenerateAccessToken(
            new UserId(user.UserId),
            user.Email,
            [RoleNames.Dietologist]);
        user.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<DietologistRelationshipPayload> GetRelationshipAsync(HttpClient client) {
        HttpResponseMessage response = await client.GetAsync("/api/v1/dietologist/relationship").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
        DietologistRelationshipPayload? relationship = await response.Content
            .ReadFromJsonAsync<DietologistRelationshipPayload>(JsonOptions)
            .ConfigureAwait(false);
        Assert.NotNull(relationship);
        return relationship;
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.Fail(string.Create(CultureInfo.InvariantCulture, $"Expected {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}"));
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken, UserPayload User);

    [ExcludeFromCodeCoverage]
    private sealed record UserPayload(Guid Id);

    [ExcludeFromCodeCoverage]
    private sealed record AuthenticatedUser(HttpClient Client, string Email, Guid UserId);

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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
