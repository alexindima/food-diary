using System.Net;
using System.Text;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using FoodDiary.Testing.Assertions;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RecipeSourceTransportTests {
    [Theory]
    [InlineData("text/plain", "  Oats and milk  ", "Oats and milk")]
    [InlineData("text/html", "<p>Oats &amp; milk</p>", "Oats & milk")]
    [InlineData("application/ld+json", "<script>{\"@type\":\"Recipe\",\"name\":\"Oats\"}</script>", "{\"@type\":\"Recipe\",\"name\":\"Oats\"}")]
    public async Task Reader_ReturnsExtractedTextAndTheFinalSourceUrl(string mediaType, string body, string expected) {
        using var handler = new SourceHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, mediaType) });
        using var http = new HttpClient(handler);

        RecipeSource result = ResultAssert.Success(await new RecipeSourceReader(http).ReadAsync("https://example.org/recipe", CancellationToken.None));

        Assert.Multiple(() => Assert.Equal(expected, result.Text), () => Assert.Equal("https://example.org/recipe", result.SourceUrl));
        Assert.Single(handler.Targets);
    }

    [Fact]
    public async Task Reader_BoundsUsableSourceTextToSixteenThousandCharacters() {
        using var handler = new SourceHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('x', 16001), Encoding.UTF8, "text/plain") });
        using var http = new HttpClient(handler);

        RecipeSource result = ResultAssert.Success(await new RecipeSourceReader(http).ReadAsync("https://example.org/recipe", CancellationToken.None));

        Assert.Equal(new string('x', 16000), result.Text);
    }

    [Fact]
    public async Task Reader_FollowsAnAllowedRelativeRedirectAndReturnsItsResolvedUrl() {
        using var handler = new SourceHandler((index, _) => index == 0
            ? new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("/final", UriKind.Relative) } }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Oats", Encoding.UTF8, "text/plain") });
        using var http = new HttpClient(handler);

        RecipeSource result = ResultAssert.Success(await new RecipeSourceReader(http).ReadAsync("https://example.org/recipe", CancellationToken.None));

        Assert.Equal("https://example.org/final", result.SourceUrl);
        Assert.Equal(2, handler.Targets.Count);
    }

    [Theory]
    [InlineData("redirect-limit", 4)]
    [InlineData("missing-location", 1)]
    [InlineData("status", 1)]
    [InlineData("media-type", 1)]
    [InlineData("empty-text", 1)]
    public async Task Reader_RejectsUnavailableOrEmptySourcesWithBoundedRequests(string failure, int expectedCalls) {
        using var handler = new SourceHandler((_, _) => failure switch {
            "redirect-limit" => new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("/again", UriKind.Relative) } },
            "missing-location" => new HttpResponseMessage(HttpStatusCode.Found),
            "status" => new HttpResponseMessage(HttpStatusCode.NotFound),
            "media-type" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("video", Encoding.UTF8, "video/mp4") },
            "empty-text" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("  ", Encoding.UTF8, "text/plain") },
            _ => throw new ArgumentOutOfRangeException(nameof(failure)),
        });
        using var http = new HttpClient(handler);

        ResultAssert.Failure(await new RecipeSourceReader(http).ReadAsync("https://example.org/recipe", CancellationToken.None), "Ai.RecipeSourceUnavailable");
        Assert.Equal(expectedCalls, handler.Targets.Count);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("io")]
    [InlineData("timeout")]
    [InlineData("uri")]
    [InlineData("internal-cancellation")]
    public async Task Reader_ConvertsTransportFailuresToAControlledResult(string failure) {
        using var handler = new SourceHandler((_, _) => throw (failure switch {
            "http" => new HttpRequestException(),
            "io" => new IOException(),
            "timeout" => new TimeoutException(),
            "uri" => new UriFormatException(),
            "internal-cancellation" => new OperationCanceledException(),
            _ => new InvalidOperationException(),
        }));
        using var http = new HttpClient(handler);

        ResultAssert.Failure(await new RecipeSourceReader(http).ReadAsync("https://example.org/recipe", CancellationToken.None), "Ai.RecipeSourceUnavailable");
    }

    [Theory]
    [InlineData("https://vk.ru/away.php")]
    [InlineData("https://vk.ru/away.php?to=not-a-url")]
    [InlineData("https://vk.ru/away.php?to=http%3A%2F%2Fexample.org")]
    public async Task Reader_RejectsInvalidWrapperTargetsBeforeSendingAnyRequest(string url) {
        using var handler = new SourceHandler((_, _) => throw new InvalidOperationException("No request expected."));
        using var http = new HttpClient(handler);

        ResultAssert.Failure(await new RecipeSourceReader(http).ReadAsync(url, CancellationToken.None), "Ai.InvalidRecipeUrl");
        Assert.Empty(handler.Targets);
    }

    [Theory]
    [InlineData("<script>not JSON</script><style>hidden</style><p> Oats &amp; milk </p><p>Oats &amp; milk</p>", "Oats & milk")]
    [InlineData("<script>{\"caption\":{\"text\":\"bad\\q\"}}</script><p>Visible recipe</p>", "Visible recipe")]
    [InlineData("<script>{\"caption\":{\"text\":\"   \"}}</script><p>Visible recipe</p>", "Visible recipe")]
    [InlineData("<meta property='og:description' content=' '><meta name='description' content='Oats &amp; milk'>", "Oats & milk")]
    public void Extraction_FallsBackToUsablePageTextWithoutScriptsStylesOrDuplicates(string html, string expected) =>
        Assert.Equal(expected, RecipeSourceTextExtractor.Extract(html));

    [ExcludeFromCodeCoverage]
    private sealed class SourceHandler(Func<int, HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler {
        public List<Uri> Targets { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Targets.Add(request.RequestUri!);
            return Task.FromResult(respond(Targets.Count - 1, request));
        }
    }
}
