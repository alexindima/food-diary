using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Recipes;

[ExcludeFromCodeCoverage]
public sealed class RecipeModerationProviderTests {
    [Fact]
    public async Task OpenAiChecksTextAndConfirmedImageWithSeparateAttributionAsync() {
        var handler = new RecordingHandler(() => OpenAiResponse());
        using var client = new HttpClient(handler);
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        var assetId = ImageAssetId.New();
        var ownerId = UserId.New();
        const string dataUrl = "data:image/png;base64,Zm9vZA==";
        images.GetDataUrlAsync(assetId, ownerId, Arg.Any<CancellationToken>()).Returns(Result.Success(dataUrl));
        var provider = new OpenAiRecipeModerationProvider(client, images, "openai-test-key");

        RecipeModerationCheck check = await provider.CheckAsync(
            new RecipeModerationContent("Soup", [new("cover", assetId.Value, "https://unused.example/soup.png")], ownerId.Value),
            CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        using var textBody = JsonDocument.Parse(handler.Requests[0].Body);
        using var imageBody = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.Multiple(
            () => Assert.All(handler.Requests, request => {
                Assert.Equal("https://api.openai.com/v1/moderations", request.Url);
                Assert.Equal("Bearer openai-test-key", request.Authorization);
            }),
            () => Assert.Equal("omni-moderation-latest", textBody.RootElement.GetProperty("model").GetString()),
            () => Assert.Equal("Soup", textBody.RootElement.GetProperty("input").GetString()),
            () => Assert.Equal(dataUrl, imageBody.RootElement.GetProperty("input")[0].GetProperty("image_url").GetProperty("url").GetString()),
            () => Assert.Equal("omni-moderation-2024-09-26", check.Model),
            () => Assert.Collection(
                check.Signals,
                signal => Assert.Equal("text", signal.Field),
                signal => Assert.Equal("cover", signal.Field)),
            () => Assert.All(check.Signals, signal => {
                Assert.Equal("modr-test", signal.RequestId);
                Assert.True(signal.Categories["violence"]);
                Assert.Equal(0.75, signal.Scores["violence"]);
                Assert.True(signal.Flagged);
            }));
    }

    [Fact]
    public async Task MistralUsesPinnedModelAndDecodesItsOwnCategoryFlagsAsync() {
        var handler = new RecordingHandler(static () => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(new {
                id = "mistral-test",
                model = "mistral-moderation-2603",
                results = new[] { new { categories = new { health = true }, category_scores = new { health = 0.8 } } },
            }),
        });
        using var client = new HttpClient(handler);
        var provider = new MistralRecipeModerationProvider(client, "mistral-test-key");

        RecipeModerationCheck check = await provider.CheckAsync(new RecipeModerationContent("Soup", []), CancellationToken.None);

        RecordedRequest request = Assert.Single(handler.Requests);
        RecipeModerationSignal signal = Assert.Single(check.Signals);
        using var body = JsonDocument.Parse(request.Body);
        Assert.Multiple(
            () => Assert.Equal("https://api.mistral.ai/v1/moderations", request.Url),
            () => Assert.Equal("Bearer mistral-test-key", request.Authorization),
            () => Assert.Equal("mistral-moderation-2603", body.RootElement.GetProperty("model").GetString()),
            () => Assert.Equal("Soup", body.RootElement.GetProperty("input").GetString()),
            () => Assert.True(signal.Flagged),
            () => Assert.Equal(0.8, signal.Scores["health"]));
    }

    [Fact]
    public void RegistrationKeepsMistralUnavailableWithoutAKey() {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("OpenAi:ApiKey", "openai-test-key")])
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IImageAssetContentService>());
        services.AddRecipeModerationProviders(configuration);
        using ServiceProvider serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        IRecipeModerationProvider[] providers = [.. serviceProvider.GetServices<IRecipeModerationProvider>()];

        Assert.True(Assert.Single(providers, static provider => string.Equals(provider.Name, "OpenAi", StringComparison.Ordinal)).IsAvailable);
        Assert.False(Assert.Single(providers, static provider => string.Equals(provider.Name, "Mistral", StringComparison.Ordinal)).IsAvailable);
    }

    [Fact]
    public async Task OpenAiRefusesUnmanagedImageBeforeCallingProviderAsync() {
        var handler = new RecordingHandler(() => OpenAiResponse());
        using var client = new HttpClient(handler);
        var provider = new OpenAiRecipeModerationProvider(client, Substitute.For<IImageAssetContentService>(), "openai-test-key");
        var content = new RecipeModerationContent("Soup", [new("cover", AssetId: null, "https://unused.example/soup.png")], Guid.NewGuid());

        RecipeModerationProviderException exception = await Assert.ThrowsAsync<RecipeModerationProviderException>(
            () => provider.CheckAsync(content, CancellationToken.None));

        Assert.False(exception.Retryable);
        Assert.True(exception.RequiresContentChanges);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    public async Task ProviderErrorsExposeOnlySafeRetryMetadataAsync(HttpStatusCode status, bool retryable) {
        var handler = new RecordingHandler(() => {
            var response = new HttpResponseMessage(status) { Content = new StringContent("upstream private details") };
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(37));
            return response;
        });
        using var client = new HttpClient(handler);
        var provider = new MistralRecipeModerationProvider(client, "mistral-test-key");

        RecipeModerationProviderException exception = await Assert.ThrowsAsync<RecipeModerationProviderException>(
            () => provider.CheckAsync(new RecipeModerationContent("Soup", []), CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(retryable, exception.Retryable),
            () => Assert.Equal(retryable ? TimeSpan.FromSeconds(37) : (TimeSpan?)null, exception.RetryAfter),
            () => Assert.DoesNotContain("upstream private details", exception.Message, StringComparison.Ordinal),
            () => Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task OpenAiTreatsAnIncompleteResponseAsRetryableInsteadOfClearedAsync() {
        var handler = new RecordingHandler(() => OpenAiResponse(omitRequiredCategory: true));
        using var client = new HttpClient(handler);
        var provider = new OpenAiRecipeModerationProvider(client, Substitute.For<IImageAssetContentService>(), "openai-test-key");

        RecipeModerationProviderException exception = await Assert.ThrowsAsync<RecipeModerationProviderException>(
            () => provider.CheckAsync(new RecipeModerationContent("Soup", []), CancellationToken.None));

        Assert.True(exception.Retryable);
    }

    private static HttpResponseMessage OpenAiResponse(bool omitRequiredCategory = false) {
        string[] knownCategories = [
            "harassment", "harassment/threatening", "hate", "hate/threatening", "illicit", "illicit/violent",
            "self-harm", "self-harm/intent", "self-harm/instructions", "sexual", "sexual/minors", "violence", "violence/graphic",
        ];
        Dictionary<string, bool> categories = knownCategories.ToDictionary(
            static category => category, IsViolence, StringComparer.Ordinal);
        if (omitRequiredCategory) {
            categories.Remove("violence");
        }

        var result = new {
            flagged = true,
            categories,
            category_scores = knownCategories.ToDictionary(
                static category => category, static category => IsViolence(category) ? 0.75 : 0, StringComparer.Ordinal),
        };
        return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(new {
                id = "modr-test",
                model = "omni-moderation-2024-09-26",
                results = new[] { result },
            }),
        };

        static bool IsViolence(string category) => string.Equals(category, "violence", StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Assert.NotNull(request.Content);
            string body = await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(request.RequestUri?.AbsoluteUri, request.Headers.Authorization?.ToString(), body));
            return respond();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record RecordedRequest(string? Url, string? Authorization, string Body);
}
