using System.Net;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RecipeSourceReaderTests {
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("100.64.0.1")]
    [InlineData("198.18.0.1")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    [InlineData("2001:db8::1")]
    [InlineData("2002:7f00:1::")]
    public void Addresses_PrivateOrSpecial_AreRejected(string address) =>
        Assert.False(RecipeSourceReader.IsPublicAddress(IPAddress.Parse(address)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void Addresses_Public_AreAllowed(string address) =>
        Assert.True(RecipeSourceReader.IsPublicAddress(IPAddress.Parse(address)));

    [Theory]
    [InlineData("http://example.org/recipe")]
    [InlineData("https://user:password@example.org/recipe")]
    [InlineData("https://example.org:8080/recipe")]
    [InlineData("file:///recipe")]
    public void Url_UnsafeSchemeOrAuthority_IsRejected(string url) => Assert.False(RecipeSourceReader.TryNormalizeUrl(url, out _));

    [Fact]
    public void Url_VkWrapper_ResolvesAndRemovesInstagramTracking() {
        Assert.True(RecipeSourceReader.TryNormalizeUrl("https://vk.ru/away.php?to=https%3A%2F%2Fwww.instagram.com%2Freel%2Fexample%2F%3Fstkn%3Dtracking&utf=1", out Uri? url));
        Assert.Equal("https://www.instagram.com/reel/example/", url!.AbsoluteUri);
    }

    [Fact]
    public void Extraction_RecipeGraph_PrefersRecipeOverMarketing() {
        const string html = """
            <meta property="og:description" content="Follow for more!">
            <script type="application/ld+json">{"@graph":[{"@type":"WebPage"},{"@type":["Thing","Recipe"],"name":"Salad","recipeIngredient":["180g yogurt","pinch salt"],"recipeInstructions":["Mix"]}]}</script>
            """;
        string text = RecipeSourceTextExtractor.Extract(html);
        Assert.Multiple(() => Assert.Contains("180g yogurt", text, StringComparison.Ordinal),
            () => Assert.Contains("pinch salt", text, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Follow for more", text, StringComparison.Ordinal));
    }

    [Fact]
    public void Extraction_EmbeddedCaption_DecodesEscapes() {
        string text = RecipeSourceTextExtractor.Extract("<script>{\"caption\":{\"text\":\"Salad\\n180g yoghurt\\nPinch of salt\"}}</script>");
        Assert.Equal("Salad\n180g yoghurt\nPinch of salt", text);
    }

    [Fact]
    public void Extraction_MetaDescription_PreservesApostrophesAndEntities() {
        string text = RecipeSourceTextExtractor.Extract("<meta content=\"Author's salad &amp; yoghurt\" property=\"og:description\">");
        Assert.Equal("Author's salad & yoghurt", text);
    }

    [Fact]
    public void Extraction_PageDescription_PrefersRequestedRecipeOverRecommendedCaption() {
        const string html = """
            <meta property="og:description" content="Creamy Cucumber Salad&#10;180g yoghurt&#10;&#xbd; cucumber&#10;Mix and serve">
            <script>{"caption":{"text":"Recommended tuna salad\n1 can tuna\nMix"}}</script>
            <div>More posts: Chocolate cake</div>
            """;
        Assert.Equal("Creamy Cucumber Salad\n180g yoghurt\n½ cucumber\nMix and serve", RecipeSourceTextExtractor.Extract(html));
    }

    [Fact]
    public void Extraction_OpenGraphDescription_PrefersPostOverGenericSiteDescription() {
        const string html = """
            <meta name="description" content="Discover more recipes on Instagram">
            <meta property="og:description" content="Cucumber Salad: 180g yoghurt, mix and serve">
            <script>{"caption":{"text":"A different recommended post"}}</script>
            """;
        Assert.Equal("Cucumber Salad: 180g yoghurt, mix and serve", RecipeSourceTextExtractor.Extract(html));
    }

    [Fact]
    public void Extraction_RepeatedSameCaption_RemainsUsable() {
        const string html = """
            <script>{"caption":{"text":"Salad\n180g yoghurt"},"duplicate":{"caption":{"text":"Salad\n180g yoghurt"}}}</script>
            """;
        Assert.Equal("Salad\n180g yoghurt", RecipeSourceTextExtractor.Extract(html));
    }

    [Fact]
    public async Task Reader_AmbiguousCaptionsWithoutPageDescription_RejectsSource() {
        const string html = """
            <script>{"posts":[{"caption":{"text":"Tuna salad: 1 can tuna"}},{"caption":{"text":"Cake: 100g flour"}}]}</script>
            <div>Suggested recipes</div>
            """;
        using var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html"),
        });
        using var http = new HttpClient(handler);
        Result<RecipeSource> result = await new RecipeSourceReader(http).ReadAsync("https://www.instagram.com/reel/example/", CancellationToken.None);
        ResultAssert.Failure(result, "Ai.RecipeSourceUnavailable");
    }

    [Fact]
    public async Task Reader_ActualPrivateDestination_IsBlockedAtConnection() {
        using var handler = new SocketsHttpHandler { UseProxy = false, ConnectCallback = RecipeSourceReader.ConnectPublicAsync };
        using var http = new HttpClient(handler);
        Result<RecipeSource> result = await new RecipeSourceReader(http).ReadAsync("https://127.0.0.1/private", CancellationToken.None);
        ResultAssert.Failure(result, "Ai.RecipeSourceUnavailable");
    }

    [Fact]
    public async Task Reader_UnsafeRedirect_StopsBeforeFollowing() {
        using var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("http://localhost/private") } });
        using var http = new HttpClient(handler);
        Result<RecipeSource> result = await new RecipeSourceReader(http).ReadAsync("https://example.org/recipe", CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Reader_OversizedPage_IsRejected() {
        using var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('x', (1024 * 1024) + 1), System.Text.Encoding.UTF8, "text/html") });
        using var http = new HttpClient(handler);
        Result<RecipeSource> result = await new RecipeSourceReader(http).ReadAsync("https://example.org/recipe", CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Ai.RecipeSourceUnavailable", result.Error.Code);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Calls++;
            return Task.FromResult(response);
        }
    }
}
