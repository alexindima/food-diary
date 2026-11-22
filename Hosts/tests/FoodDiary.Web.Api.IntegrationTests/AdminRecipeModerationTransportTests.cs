using System.Net;
using System.Text.Json;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AdminRecipeModerationTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    private const string Endpoint = "/api/v1/admin/moderation/recipes";
    private const string Preview = "data:image/webp;base64,UklGRg==";

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Support", HttpStatusCode.Forbidden)]
    public async Task ImagePreview_RequiresAdministratorAsync(string? role, HttpStatusCode expected) {
        using HttpClient client = factory.CreateClient();
        if (role is not null) {
            Authenticate(client, role);
        }

        using HttpResponseMessage response = await client.GetAsync($"{Endpoint}/{Guid.NewGuid()}/images/{Guid.NewGuid()}");
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task ImagePreview_BindsRevisionAndAssetAndDisablesCachingAsync() {
        var revisionId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        IRecipeModerationAdministration administration = Substitute.For<IRecipeModerationAdministration>();
        administration.GetImageAsync(revisionId, assetId, Arg.Any<CancellationToken>()).Returns(Preview);
        await using WebApplicationFactory<Program> testHost = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.RemoveAll<IRecipeModerationAdministration>();
            services.AddSingleton(administration);
        }));
        using HttpClient client = testHost.CreateClient();
        Authenticate(client, PresentationRoleNames.Admin);

        using HttpResponseMessage response = await client.GetAsync($"{Endpoint}/{revisionId}/images/{assetId}");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(Preview, document.RootElement.GetProperty("dataUrl").GetString());
        await administration.Received(1).GetImageAsync(revisionId, assetId, Arg.Any<CancellationToken>());
    }

    private static void Authenticate(HttpClient client, string role) {
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
    }
}
