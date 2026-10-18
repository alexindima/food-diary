using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Dietologist.Presentation.Requests;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
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

    [RequiresDockerFact]
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
        SetDietologistToken(dietologistUser);

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

    [RequiresDockerFact]
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

    [RequiresDockerFact]
    public async Task PermissionsAndDisconnect_TakeEffectOnNextDietologistApiRequest() {
        await EnsureDietologistRoleAsync();
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync();
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");
        await RefreshAccessTokenAsync(dietologistUser);

        HttpResponseMessage inviteResponse = await clientUser.Client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(dietologistUser.Email, new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse);
        DietologistRelationshipPayload relationship = await GetRelationshipAsync(clientUser.Client);
        HttpResponseMessage acceptResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/accept-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, acceptResponse);
        await RefreshAccessTokenAsync(dietologistUser);

        string dashboardUrl = $"/api/v1/dietologist/clients/{clientUser.UserId}/dashboard?dateFrom=2026-07-01&dateTo=2026-07-14&page=1&pageSize=10&trendDays=14&locale=en";
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
        await RefreshAccessTokenAsync(dietologistUser);

        HttpResponseMessage revokedPermissionResponse = await dietologistUser.Client.GetAsync(dashboardUrl);
        Assert.Equal(HttpStatusCode.Forbidden, revokedPermissionResponse.StatusCode);

        HttpResponseMessage restoreResponse = await clientUser.Client.PutAsJsonAsync(
            "/api/v1/dietologist/permissions",
            new UpdateDietologistPermissionsHttpRequest(new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, restoreResponse);
        await RefreshAccessTokenAsync(dietologistUser);
        await AssertStatusCodeAsync(HttpStatusCode.OK, await dietologistUser.Client.GetAsync(dashboardUrl));

        HttpResponseMessage disconnectResponse = await clientUser.Client.DeleteAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, disconnectResponse);
        await RefreshAccessTokenAsync(dietologistUser);
        HttpResponseMessage disconnectedResponse = await dietologistUser.Client.GetAsync(dashboardUrl);
        Assert.Equal(HttpStatusCode.Forbidden, disconnectedResponse.StatusCode);
    }

    [RequiresDockerFact]
    public async Task GoalsAccess_ExposesOnlyGoalsAndRechecksPermissionsAsync() {
        await EnsureDietologistRoleAsync();
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync();
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");
        AuthenticatedUser unrelatedUser = await CreateAuthenticatedClientAsync("unrelated-dietologist");
        SetDietologistToken(unrelatedUser);

        HttpResponseMessage goalsUpdate = await clientUser.Client.PatchAsJsonAsync("/api/v1/goals", new {
            dailyCalorieTarget = 2100,
            proteinTarget = 125,
            fatTarget = 60,
            carbTarget = 240,
            fiberTarget = 0,
            waterGoal = 1800,
            desiredWeightKg = 65.5,
            desiredWaistCm = 78,
        });
        await AssertStatusCodeAsync(HttpStatusCode.OK, goalsUpdate);
        HttpResponseMessage profileUpdate = await clientUser.Client.PatchAsJsonAsync("/api/v1/users/info", new {
            firstName = "Private",
            lastName = "Profile",
            birthDate = "1991-02-03",
            heightCm = 181,
            stepGoal = 6000,
            hydrationGoal = 2600,
        });
        await AssertStatusCodeAsync(HttpStatusCode.OK, profileUpdate);

        var permissions = new DietologistPermissionsHttpRequest(
            ShareMeals: false, ShareStatistics: false, ShareWeight: false, ShareWaist: false,
            ShareGoals: true, ShareHydration: false, ShareProfile: true, ShareFasting: false);
        HttpResponseMessage invite = await clientUser.Client.PostAsJsonAsync("/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(dietologistUser.Email, permissions));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, invite);
        DietologistRelationshipPayload relationship = await GetRelationshipAsync(clientUser.Client);
        SetDietologistToken(dietologistUser);
        HttpResponseMessage pendingRead = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/clients/{clientUser.UserId}/goals");
        Assert.Equal(HttpStatusCode.Forbidden, pendingRead.StatusCode);
        HttpResponseMessage accept = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/accept-current-user", new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, accept);
        SetDietologistToken(dietologistUser);

        await AssertGoalsOnlyPayloadAsync(dietologistUser.Client, clientUser.UserId);
        HttpResponseMessage unrelatedRead = await unrelatedUser.Client.GetAsync($"/api/v1/dietologist/clients/{clientUser.UserId}/goals");
        Assert.Equal(HttpStatusCode.Forbidden, unrelatedRead.StatusCode);

        HttpResponseMessage revokeProfile = await clientUser.Client.PutAsJsonAsync("/api/v1/dietologist/permissions",
            new UpdateDietologistPermissionsHttpRequest(permissions with { ShareProfile = false }));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, revokeProfile);
        HttpResponseMessage newPrivateProfile = await clientUser.Client.PatchAsJsonAsync("/api/v1/users/info", new {
            firstName = "New private name",
            birthDate = "2001-01-01",
            heightCm = 182,
        });
        await AssertStatusCodeAsync(HttpStatusCode.OK, newPrivateProfile);
        await AssertGoalsOnlyPayloadAsync(dietologistUser.Client, clientUser.UserId);

        foreach (bool shareProfile in new[] { false, true }) {
            HttpResponseMessage revokeGoals = await clientUser.Client.PutAsJsonAsync("/api/v1/dietologist/permissions",
                new UpdateDietologistPermissionsHttpRequest(permissions with { ShareGoals = false, ShareProfile = shareProfile }));
            await AssertStatusCodeAsync(HttpStatusCode.NoContent, revokeGoals);
            HttpResponseMessage denied = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/clients/{clientUser.UserId}/goals");
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        HttpResponseMessage restoreGoals = await clientUser.Client.PutAsJsonAsync("/api/v1/dietologist/permissions",
            new UpdateDietologistPermissionsHttpRequest(permissions with { ShareProfile = false }));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, restoreGoals);
        await AssertGoalsOnlyPayloadAsync(dietologistUser.Client, clientUser.UserId);
        HttpResponseMessage disconnect = await clientUser.Client.DeleteAsync("/api/v1/dietologist/relationship");
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, disconnect);
        HttpResponseMessage disconnectedRead = await dietologistUser.Client.GetAsync($"/api/v1/dietologist/clients/{clientUser.UserId}/goals");
        Assert.Equal(HttpStatusCode.Forbidden, disconnectedRead.StatusCode);
    }

    [RequiresDockerFact]
    public async Task UnconfirmedRecipient_CannotReadAcceptOrDeclineCurrentUserInvitationAsync() {
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync();
        AuthenticatedUser recipient = await CreateAuthenticatedClientAsync("unconfirmed-recipient", confirmEmail: false);
        HttpResponseMessage invite = await clientUser.Client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(recipient.Email, new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, invite);
        DietologistRelationshipPayload relationship = await GetRelationshipAsync(clientUser.Client);
        string invitationUrl = $"/api/v1/dietologist/invitations/{relationship.InvitationId}";

        HttpResponseMessage read = await recipient.Client.GetAsync($"{invitationUrl}/current-user");
        await AssertStatusCodeAsync(HttpStatusCode.Forbidden, read);
        HttpResponseMessage accept = await recipient.Client.PostAsJsonAsync($"{invitationUrl}/accept-current-user", new { });
        await AssertStatusCodeAsync(HttpStatusCode.Forbidden, accept);
        HttpResponseMessage decline = await recipient.Client.PostAsJsonAsync($"{invitationUrl}/decline-current-user", new { });
        await AssertStatusCodeAsync(HttpStatusCode.Forbidden, decline);

        Assert.Equal("Pending", (await GetRelationshipAsync(clientUser.Client)).Status);
    }

    private static async Task AssertGoalsOnlyPayloadAsync(HttpClient client, Guid clientUserId) {
        HttpResponseMessage response = await client.GetAsync($"/api/v1/dietologist/clients/{clientUserId}/goals");
        await AssertStatusCodeAsync(HttpStatusCode.OK, response);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement goals = body.RootElement;
        string[] allowedProperties = [
            "id", "dailyCalorieTarget", "proteinTarget", "fatTarget", "carbTarget", "fiberTarget",
            "waterGoal", "hydrationGoal", "desiredWeightKg", "desiredWaistCm", "stepGoal",
        ];
        Assert.Equal(allowedProperties.Order(StringComparer.Ordinal),
            goals.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.Equal(clientUserId, goals.GetProperty("id").GetGuid());
        Assert.Equal(2100, goals.GetProperty("dailyCalorieTarget").GetDouble());
        Assert.Equal(125, goals.GetProperty("proteinTarget").GetDouble());
        Assert.Equal(60, goals.GetProperty("fatTarget").GetDouble());
        Assert.Equal(240, goals.GetProperty("carbTarget").GetDouble());
        Assert.Equal(0, goals.GetProperty("fiberTarget").GetDouble());
        Assert.Equal(1800, goals.GetProperty("waterGoal").GetDouble());
        Assert.Equal(2600, goals.GetProperty("hydrationGoal").GetDouble());
        Assert.Equal(65.5, goals.GetProperty("desiredWeightKg").GetDouble());
        Assert.Equal(78, goals.GetProperty("desiredWaistCm").GetDouble());
        Assert.Equal(6000, goals.GetProperty("stepGoal").GetInt32());
    }

    private async Task<AuthenticatedUser> CreateAuthenticatedClientAsync(string emailPrefix = "api-tests", bool confirmEmail = true) {
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
        var registeredUser = new AuthenticatedUser(client, email, authPayload.User.Id);
        if (confirmEmail) {
            string token = factory.EmailSender.GetRequiredEmailVerificationToken(email);
            HttpResponseMessage verification = await client.PostAsJsonAsync(
                "/api/v1/auth/verify-email",
                new { registeredUser.UserId, Token = token }).ConfigureAwait(false);
            await AssertStatusCodeAsync(HttpStatusCode.NoContent, verification).ConfigureAwait(false);
            await RefreshAccessTokenAsync(registeredUser).ConfigureAwait(false);
        }

        return registeredUser;
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
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        long securityVersion = dbContext.Users
            .Where(candidate => candidate.Id == new UserId(user.UserId))
            .Select(candidate => candidate.SecurityVersion)
            .Single();
        string token = tokenGenerator.GenerateAccessToken(
            new UserId(user.UserId),
            user.Email,
            [RoleNames.Dietologist],
            expiresAtUtc: null,
            securityVersion,
            new FoodDiary.Modules.Identity.Domain.ValueObjects.Ids.RefreshTokenSessionId(Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .ReadJwtToken(user.Client.DefaultRequestHeaders.Authorization!.Parameter!)
                .Claims.Single(claim => string.Equals(claim.Type, "refresh_session_id", StringComparison.Ordinal)).Value)));
        user.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task RefreshAccessTokenAsync(AuthenticatedUser user) {
        HttpResponseMessage response = await user.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginHttpRequest(user.Email, "Password123!")).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        AuthPayload? authPayload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(authPayload);
        user.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
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
