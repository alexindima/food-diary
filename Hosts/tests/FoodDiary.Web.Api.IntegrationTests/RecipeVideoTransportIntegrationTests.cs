using System.Net;
using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeVideoTransportIntegrationTests(TransportApiWebApplicationFactory factory) : IClassFixture<TransportApiWebApplicationFactory> {
    private const string Endpoint = "/api/v1/ai/food/recipe-import/video";

    [Fact]
    public async Task SwaggerVideoEndpoint_HasMultipartContract_AndMatchesFullSnapshot() {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement operation = document.RootElement["paths"]["/api/v{version}/ai/food/recipe-import/video"]["post"];
        JsonElement form = operation["requestBody"]["content"]["multipart/form-data"]["schema"]["properties"];
        Assert.Multiple(
            () => Assert.True(operation["requestBody"]["content"].TryGetProperty("multipart/form-data", out _)),
            () => Assert.Equal("binary", form["video"]["format"].GetString()),
            () => Assert.Equal("string", form["sourceUrl"]["type"].GetString()),
            () => Assert.Equal("string", form["text"]["type"].GetString()),
            () => Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _)),
            () => Assert.True(operation.GetProperty("responses").TryGetProperty("413", out _)),
            () => Assert.True(operation.GetProperty("responses").TryGetProperty("429", out _)));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-full-contract.json", PresentationBoundaryIntegrationTests.BuildFullOpenApiSnapshot(document.RootElement));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-focused-contract.json", PresentationBoundaryIntegrationTests.BuildFocusedOpenApiSnapshot(document.RootElement));
    }

    [Fact]
    public async Task MultipartVideo_WithoutIdempotencyKey_ReturnsStandardErrorBeforeUseCase() {
        using HttpClient client = factory.CreateClient();
        Authenticate(client, "Premium");
        using var content = new MultipartFormDataContent {
            { new ByteArrayContent([1, 2, 3]), "video", "recipe.mp4" },
            { new StringContent("caption"), "text" },
        };
        using HttpResponseMessage response = await client.PostAsync(Endpoint, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Idempotency.Required", document.RootElement.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("User", HttpStatusCode.Forbidden)]
    public async Task VideoEndpoint_RequiresPremium(string? role, HttpStatusCode expected) {
        using HttpClient client = factory.CreateClient();
        if (role is not null) {
            Authenticate(client, role);
        }
        using var content = new MultipartFormDataContent { { new StringContent("https://example.org/video"), "sourceUrl" } };
        using HttpResponseMessage response = await client.PostAsync(Endpoint, content);
        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid-source")]
    [InlineData("invalid-file")]
    [InlineData("source-failure")]
    [InlineData("transcription-failure")]
    public async Task VideoEndpoint_MapsImportFailuresToThePublishedContractAsync(string scenario) {
        Error error = scenario switch {
            "invalid-source" => AiErrors.InvalidRecipeVideoSource(),
            "invalid-file" => AiErrors.InvalidRecipeVideo(),
            "source-failure" => AiErrors.RecipeVideoUnavailable(),
            "transcription-failure" => AiErrors.OpenAiFailed("Private provider diagnostics."),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<RecipeImportDraftModel>>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RecipeImportDraftModel>(error));
        await using WebApplicationFactory<Program> testHost = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => {
            services.RemoveAll<ISender>();
            services.AddSingleton(sender);
        }));
        using HttpClient client = testHost.CreateClient();
        Authenticate(client, "Premium");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var content = new MultipartFormDataContent { { new StringContent("https://example.org/video"), "sourceUrl" } };

        using HttpResponseMessage response = await client.PostAsync(Endpoint, content);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var snapshots = JsonDocument.Parse(await File.ReadAllTextAsync(SnapshotPathResolver.GetPath("recipe-video-error-contract.json")));
        JsonElement expected = snapshots.RootElement.GetProperty(scenario);
        Assert.Multiple(
            () => Assert.Equal(expected.GetProperty("status").GetInt32(), (int)response.StatusCode),
            () => Assert.Equal(expected.GetProperty("error").GetString(), document.RootElement.GetProperty("error").GetString()),
            () => Assert.Equal(expected.GetProperty("message").GetString(), document.RootElement.GetProperty("message").GetString()));
    }

    private static void Authenticate(HttpClient client, string role) {
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
    }
}
