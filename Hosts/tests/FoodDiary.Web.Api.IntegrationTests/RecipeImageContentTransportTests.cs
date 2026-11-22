using System.Net;
using System.Text.Json;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeImageContentTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    private const string Preview = "data:image/webp;base64,UklGRg==";

    [Fact]
    public async Task ContentPreview_AnonymousRequestIsRejectedAsync() {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync($"/api/v1/images/{Guid.NewGuid()}/content");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ContentPreview_UsesAuthenticatedOwnerAndDisablesCachingAsync() {
        var asset = ImageAssetId.New();
        var owner = UserId.New();
        var visitor = UserId.New();
        IImageAssetContentService content = Substitute.For<IImageAssetContentService>();
        content.GetDataUrlAsync(asset, owner, Arg.Any<CancellationToken>()).Returns(Result.Success(Preview));
        content.GetDataUrlAsync(asset, visitor, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(ImageErrors.NotFound(asset.Value)));
        await using WebApplicationFactory<Program> host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.RemoveAll<IImageAssetContentService>();
            services.AddSingleton(content);
        }));
        using HttpClient ownerClient = CreateAuthenticatedClient(host, owner);
        using HttpClient visitorClient = CreateAuthenticatedClient(host, visitor);
        string requestUri = $"/api/v1/images/{asset.Value}/content";

        using HttpResponseMessage ownerResponse = await ownerClient.GetAsync(requestUri);
        using HttpResponseMessage visitorResponse = await visitorClient.GetAsync(requestUri);
        using var payload = JsonDocument.Parse(await ownerResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.True(ownerResponse.Headers.CacheControl?.NoStore);
        Assert.Equal(Preview, payload.RootElement.GetProperty("dataUrl").GetString());
        Assert.Equal(HttpStatusCode.NotFound, visitorResponse.StatusCode);
        await content.Received(1).GetDataUrlAsync(asset, owner, Arg.Any<CancellationToken>());
        await content.Received(1).GetDataUrlAsync(asset, visitor, Arg.Any<CancellationToken>());
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> host, UserId userId) {
        HttpClient client = host.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.Value.ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "User");
        return client;
    }
}
