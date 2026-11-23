using System.Net;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Infrastructure.Tests.Recipes;

public sealed partial class RecipeModerationProviderTests {
    [Theory]
    [InlineData(null, "Soup", false)]
    [InlineData("fixture-key", " ", false)]
    [InlineData("fixture-key", "Soup", true)]
    public async Task MistralRejectsUnavailableConfigurationOrUnsupportedContentAsync(string? key, string text, bool includePhoto) {
        var handler = new RecordingHandler(() => throw new InvalidOperationException("Unexpected provider call."));
        using var client = new HttpClient(handler);
        var provider = new MistralRecipeModerationProvider(client, key);
        var content = new RecipeModerationContent(text, includePhoto ? [new("cover", Guid.NewGuid(), string.Empty)] : []);

        RecipeModerationProviderException error = await Assert.ThrowsAsync<RecipeModerationProviderException>(
            () => provider.CheckAsync(content, CancellationToken.None));

        Assert.Multiple(
            () => Assert.False(error.Retryable),
            () => Assert.False(error.RequiresContentChanges),
            () => Assert.Empty(handler.Requests));
    }

    [Theory]
    [InlineData(null, "Soup")]
    [InlineData("fixture-key", " ")]
    public async Task OpenAiRejectsUnavailableConfigurationOrEmptyContentAsync(string? key, string text) {
        var handler = new RecordingHandler(() => throw new InvalidOperationException("Unexpected provider call."));
        using var client = new HttpClient(handler);
        var provider = new OpenAiRecipeModerationProvider(client, Substitute.For<IImageAssetContentService>(), key);

        RecipeModerationProviderException error = await Assert.ThrowsAsync<RecipeModerationProviderException>(
            () => provider.CheckAsync(new RecipeModerationContent(text, []), CancellationToken.None));

        Assert.Multiple(
            () => Assert.False(error.Retryable),
            () => Assert.False(error.RequiresContentChanges),
            () => Assert.Empty(handler.Requests));
    }

    [Theory]
    [InlineData(ErrorKind.ExternalFailure, true)]
    [InlineData(ErrorKind.NotFound, false)]
    public async Task OpenAiSeparatesTemporaryImageReadFailuresFromImagesNeedingReplacementAsync(ErrorKind kind, bool retryable) {
        var handler = new RecordingHandler(() => throw new InvalidOperationException("Unexpected provider call."));
        using var client = new HttpClient(handler);
        var assetId = ImageAssetId.New();
        var ownerId = UserId.New();
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        images.GetDataUrlAsync(assetId, ownerId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(new Error("Images.FixtureFailure", "Private storage detail.", kind)));
        var provider = new OpenAiRecipeModerationProvider(client, images, "fixture-key");

        RecipeModerationProviderException error = await Assert.ThrowsAsync<RecipeModerationProviderException>(() => provider.CheckAsync(
            new RecipeModerationContent(string.Empty, [new("cover", assetId.Value, string.Empty)], ownerId.Value), CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(retryable, error.Retryable),
            () => Assert.Equal(!retryable, error.RequiresContentChanges),
            () => Assert.DoesNotContain("Private storage detail", error.Message, StringComparison.Ordinal),
            () => Assert.Empty(handler.Requests));
    }

    [Fact]
    public async Task OpenAiDoesNotCombineChecksFromDifferentModelVersionsAsync() {
        int requestCount = 0;
        var handler = new RecordingHandler(() => ++requestCount == 1 ? OpenAiResponse() : OpenAiResponse(model: "omni-moderation-fixture"));
        using var client = new HttpClient(handler);
        var assetId = ImageAssetId.New();
        var ownerId = UserId.New();
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        images.GetDataUrlAsync(assetId, ownerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success("data:image/png;base64,Zm9vZA=="));
        var provider = new OpenAiRecipeModerationProvider(client, images, "fixture-key");

        RecipeModerationProviderException error = await Assert.ThrowsAsync<RecipeModerationProviderException>(() => provider.CheckAsync(
            new RecipeModerationContent("Soup", [new("cover", assetId.Value, string.Empty)], ownerId.Value), CancellationToken.None));

        Assert.Multiple(
            () => Assert.True(error.Retryable),
            () => Assert.Contains("different model versions", error.Message, StringComparison.Ordinal),
            () => Assert.Equal(2, handler.Requests.Count));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("""{"id":"fixture","model":"fixture","results":[{"categories":{"health":true},"category_scores":{"other":0.8}}]}""")]
    [InlineData("""{"id":"fixture","model":"fixture","results":[{"categories":{"health":null},"category_scores":{"health":0.8}}]}""")]
    [InlineData("""{"id":"fixture","model":"fixture","results":[{"categories":{"health":true},"category_scores":{"health":1.1}}]}""")]
    public async Task MistralRejectsMalformedResultsWithoutReturningAClearSignalAsync(string body) {
        var handler = new RecordingHandler(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        using var client = new HttpClient(handler);
        var provider = new MistralRecipeModerationProvider(client, "fixture-key");

        RecipeModerationProviderException error = await Assert.ThrowsAsync<RecipeModerationProviderException>(
            () => provider.CheckAsync(new RecipeModerationContent("Soup", []), CancellationToken.None));

        Assert.Multiple(
            () => Assert.True(error.Retryable),
            () => Assert.False(error.RequiresContentChanges),
            () => Assert.Null(error.RetryAfter),
            () => Assert.Single(handler.Requests));
    }

    [Theory]
    [InlineData(nameof(OperationCanceledException), "Moderation provider timed out.")]
    [InlineData(nameof(TimeoutException), "Moderation provider timed out.")]
    [InlineData(nameof(HttpRequestException), "Moderation provider connection failed.")]
    [InlineData(nameof(IOException), "Moderation provider connection failed.")]
    public async Task TransportFailuresBecomeSafeRetryableProviderFailuresAsync(string exceptionType, string expectedMessage) {
        var handler = new RecordingHandler(() => {
            const string privateDetail = "Private upstream detail.";
            Exception failure = exceptionType switch {
                nameof(OperationCanceledException) => new OperationCanceledException(privateDetail),
                nameof(TimeoutException) => new TimeoutException(privateDetail),
                nameof(HttpRequestException) => new HttpRequestException(privateDetail),
                nameof(IOException) => new IOException(privateDetail),
                _ => throw new ArgumentOutOfRangeException(nameof(exceptionType)),
            };
            throw failure;
        });
        using var client = new HttpClient(handler);
        var provider = new MistralRecipeModerationProvider(client, "fixture-key");

        RecipeModerationProviderException error = await Assert.ThrowsAsync<RecipeModerationProviderException>(
            () => provider.CheckAsync(new RecipeModerationContent("Soup", []), CancellationToken.None));

        Assert.Multiple(
            () => Assert.True(error.Retryable),
            () => Assert.Equal(expectedMessage, error.Message),
            () => Assert.Null(error.RetryAfter),
            () => Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task ProviderRejectsAnAdvertisedOversizedResponseAsync() {
        var handler = new RecordingHandler(() => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{}") { Headers = { ContentLength = 10 * 1024 * 1024 } },
        });
        using var client = new HttpClient(handler);
        var provider = new MistralRecipeModerationProvider(client, "fixture-key");

        RecipeModerationProviderException error = await Assert.ThrowsAsync<RecipeModerationProviderException>(
            () => provider.CheckAsync(new RecipeModerationContent("Soup", []), CancellationToken.None));

        Assert.Multiple(
            () => Assert.True(error.Retryable),
            () => Assert.Equal("Moderation provider returned an incomplete or invalid result.", error.Message),
            () => Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task CallerCancellationRemainsCancellationAsync() {
        using var caller = new CancellationTokenSource();
        var handler = new RecordingHandler(() => {
            caller.Cancel();
            throw new OperationCanceledException(caller.Token);
        });
        using var client = new HttpClient(handler);
        var provider = new MistralRecipeModerationProvider(client, "fixture-key");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.CheckAsync(new RecipeModerationContent("Soup", []), caller.Token));

        Assert.Single(handler.Requests);
    }
}
