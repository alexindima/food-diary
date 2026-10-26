using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Dietologist.Presentation.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class SecurityAccessRegressionIntegrationTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions SnapshotOptions = new() { WriteIndented = true };

    [RequiresDockerFact]
    public async Task Trial_RequiresEmailVerificationBeforeSessionReceivesPremium() {
        TestUser user = await RegisterAsync(verify: false);
        using HttpResponseMessage overview = await user.Client.GetAsync("/api/v1/billing/overview");
        overview.EnsureSuccessStatusCode();
        JsonElement overviewPayload = await overview.Content.ReadFromJsonAsync<JsonElement>();
        using HttpResponseMessage rejected = await user.Client.PostAsync("/api/v1/billing/trial", content: null);
        ApiErrorHttpResponse? error = await rejected.Content.ReadFromJsonAsync<ApiErrorHttpResponse>();
        Assert.NotNull(error);
        AuthenticationPayload unverified = await RefreshAsync(user.Client);
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode),
            () => Assert.Equal("User.EmailRequired", error.Error),
            () => Assert.False(overviewPayload.GetProperty("canStartPremiumTrial").GetBoolean()),
            () => Assert.DoesNotContain(RoleNames.Premium, unverified.Roles, StringComparer.Ordinal));

        await VerifyAsync(user);
        using HttpResponseMessage started = await user.Client.PostAsync("/api/v1/billing/trial", content: null);
        started.EnsureSuccessStatusCode();
        AuthenticationPayload verified = await RefreshAsync(user.Client);
        Assert.Contains(RoleNames.Premium, verified.Roles, StringComparer.Ordinal);
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("billing-trial-verification-contract.json",
            JsonSerializer.Serialize(new {
                unverifiedTrialStatus = (int)rejected.StatusCode,
                unverifiedError = error.Error,
                unverifiedCanStartTrial = overviewPayload.GetProperty("canStartPremiumTrial").GetBoolean(),
                unverifiedTokenHasPremium = unverified.Roles.Contains(RoleNames.Premium, StringComparer.Ordinal),
                verifiedTrialStatus = (int)started.StatusCode,
                verifiedTokenHasPremium = verified.Roles.Contains(RoleNames.Premium, StringComparer.Ordinal),
            }, SnapshotOptions));
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecommendationComments_AfterEitherSideDisconnects_RejectProviderButPreserveClientHistory(bool providerDisconnects) {
        TestUser client = await RegisterAsync(verify: true);
        TestUser provider = await RegisterAsync(verify: true);
        using HttpResponseMessage invited = await client.Client.PostAsJsonAsync("/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(provider.Email, new DietologistPermissionsHttpRequest(ShareMeals: true)));
        invited.EnsureSuccessStatusCode();
        using HttpResponseMessage relationship = await client.Client.GetAsync("/api/v1/dietologist/relationship");
        relationship.EnsureSuccessStatusCode();
        JsonElement relationshipPayload = await relationship.Content.ReadFromJsonAsync<JsonElement>();
        Guid invitationId = relationshipPayload.GetProperty("invitationId").GetGuid();
        using HttpResponseMessage accepted = await provider.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{invitationId}/accept-current-user", new { });
        accepted.EnsureSuccessStatusCode();
        await RefreshAsync(provider.Client);
        using HttpResponseMessage created = await provider.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/clients/{client.Id}/recommendations", new CreateRecommendationHttpRequest("Private recommendation"));
        created.EnsureSuccessStatusCode();
        JsonElement recommendation = await created.Content.ReadFromJsonAsync<JsonElement>();
        Guid recommendationId = recommendation.GetProperty("id").GetGuid();
        string commentsUrl = $"/api/v1/recommendations/{recommendationId}/comments";
        using HttpResponseMessage firstComment = await client.Client.PostAsJsonAsync(commentsUrl,
            new CreateRecommendationCommentHttpRequest("Before disconnect"));
        firstComment.EnsureSuccessStatusCode();
        using HttpResponseMessage connected = await provider.Client.GetAsync(commentsUrl);
        connected.EnsureSuccessStatusCode();

        using HttpResponseMessage disconnected = providerDisconnects
            ? await provider.Client.DeleteAsync($"/api/v1/dietologist/clients/{client.Id}")
            : await client.Client.DeleteAsync("/api/v1/dietologist/relationship");
        Assert.Equal(HttpStatusCode.NoContent, disconnected.StatusCode);
        using HttpResponseMessage newComment = await client.Client.PostAsJsonAsync(commentsUrl,
            new CreateRecommendationCommentHttpRequest("Private comment after disconnect"));
        newComment.EnsureSuccessStatusCode();
        using HttpResponseMessage formerProvider = await provider.Client.GetAsync(commentsUrl);
        ApiErrorHttpResponse? error = await formerProvider.Content.ReadFromJsonAsync<ApiErrorHttpResponse>();
        Assert.NotNull(error);
        using HttpResponseMessage ownHistory = await client.Client.GetAsync(commentsUrl);
        ownHistory.EnsureSuccessStatusCode();
        JsonElement history = await ownHistory.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.NotFound, formerProvider.StatusCode),
            () => Assert.Equal("Dietologist.InvitationNotFound", error.Error),
            () => Assert.Contains(history.GetProperty("data").EnumerateArray(),
                item => string.Equals(item.GetProperty("text").GetString(), "Private comment after disconnect", StringComparison.Ordinal)));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("recommendation-comment-revocation-contract.json",
            JsonSerializer.Serialize(new {
                connectedReadStatus = (int)connected.StatusCode,
                disconnectStatus = (int)disconnected.StatusCode,
                formerProviderReadStatus = (int)formerProvider.StatusCode,
                error = error.Error,
                clientReadStatus = (int)ownHistory.StatusCode,
            }, SnapshotOptions));
    }

    private async Task<TestUser> RegisterAsync(bool verify) {
        HttpClient client = factory.CreateClient();
        string email = $"security-access-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        response.EnsureSuccessStatusCode();
        AuthenticationPayload? authentication = await response.Content.ReadFromJsonAsync<AuthenticationPayload>();
        Assert.NotNull(authentication);
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(response));
        var user = new TestUser(authentication.User.Id, email, client);
        if (verify) {
            await VerifyAsync(user);
        }
        return user;
    }

    private async Task VerifyAsync(TestUser user) {
        string token = factory.EmailSender.GetRequiredEmailVerificationToken(user.Email);
        using HttpResponseMessage response = await user.Client.PostAsJsonAsync("/api/v1/auth/verify-email", new { UserId = user.Id, Token = token });
        response.EnsureSuccessStatusCode();
        await RefreshAsync(user.Client);
    }

    private static async Task<AuthenticationPayload> RefreshAsync(HttpClient client) {
        using HttpResponseMessage response = await client.PostAsync("/api/v1/auth/session", content: null);
        response.EnsureSuccessStatusCode();
        AuthenticationPayload? authentication = await response.Content.ReadFromJsonAsync<AuthenticationPayload>();
        Assert.NotNull(authentication);

        return authentication;
    }

    [ExcludeFromCodeCoverage]
    private sealed record TestUser(Guid Id, string Email, HttpClient Client);

    [ExcludeFromCodeCoverage]
    private sealed record AuthenticationPayload(string SessionId, string[] Roles, UserPayload User);

    [ExcludeFromCodeCoverage]
    private sealed record UserPayload(Guid Id);
}
