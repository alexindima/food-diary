namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class GeneratedApiClientUsageTests {
    [Fact]
    public void FrontendGuard_RemainsInLocalAndCiVerification() {
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", "package.json")));
        System.Text.Json.JsonElement scripts = document.RootElement.GetProperty("scripts");
        Assert.Multiple(() => {
            Assert.Contains("check-api-client-usage.test.mjs", scripts.GetProperty("check:api-client-usage").GetString(), StringComparison.Ordinal);
            Assert.Contains("check-api-client-usage.mjs", scripts.GetProperty("check:api-client-usage").GetString(), StringComparison.Ordinal);
            Assert.Contains("npm run check:api-client-usage", scripts.GetProperty("lint").GetString(), StringComparison.Ordinal);
            Assert.Contains("npm run lint", scripts.GetProperty("verify").GetString(), StringComparison.Ordinal);
            Assert.Contains("npm run lint", File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", ".husky", "pre-push")), StringComparison.Ordinal);
            Assert.Contains("npm run lint", File.ReadAllText(ArchitectureTestPaths.FromRoot(".github", "workflows", "ci-tests.yml")), StringComparison.Ordinal);
        });
    }

    [Fact]
    public void TelegramBot_UsesGeneratedApiMethodsOutsideExplicitTransports() {
        string root = ArchitectureTestPaths.FromRoot("FoodDiary.Telegram.Bot");
        string[] paths = [.. SourceScanner.SourceFiles(root)];
        Assert.NotEmpty(paths);
        Assert.Contains(paths, path => path.Replace('\\', '/').Contains("/Api/Generated/Api/", StringComparison.Ordinal));
        Assert.Contains(paths, path => path.Replace('\\', '/').Contains("/Operations/", StringComparison.Ordinal));
        string[] violations = GeneratedApiClientUsageScanner.FindViolations(paths.Select(path => (
            Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path), File.ReadAllText(path))));
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("http.GetAsync(url)")]
    [InlineData("http.PostAsync(url, null)")]
    [InlineData("http.SendAsync(request)")]
    [InlineData("http?.GetAsync(url)")]
    [InlineData("System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<object>(http, url)")]
    [InlineData("http.GetFromJsonAsync<object>(url)")]
    public void Scanner_RejectsManualHttpWithDynamicUrls(string expression) {
        string source = $$"""
            using System.Net.Http.Json;
            class Legacy(HttpClient http) {
                void Call(string url, HttpRequestMessage request) { _ = {{expression}}; }
            }
            """;
        Assert.NotEmpty(Scan(source));
    }

    [Fact]
    public void Scanner_RejectsRenamedTypesAndDetachedMethodAliases() {
        const string source = """
            using Transport = System.Net.Http.HttpClient;
            class Legacy(Transport connection) {
                void Call() { Func<string, Task<HttpResponseMessage>> send = connection.GetAsync; }
            }
            """;
        Assert.NotEmpty(Scan(source));
    }

    [Theory]
    [InlineData("new HttpRequestMessage(HttpMethod.Get, url)")]
    [InlineData("System.Net.WebRequest.Create(url)")]
    [InlineData("new System.Net.WebClient()")]
    public void Scanner_RejectsAlternativeRawRequests(string expression) {
        Assert.NotEmpty(Scan($$"""
            class Legacy { void Call(string url) { _ = {{expression}}; } }
            """));
    }

    [Theory]
    [InlineData("\"/api/v1/users\"")]
    [InlineData("$\"/api/v1/users/{id}\"")]
    public void Scanner_RejectsHandwrittenApiRoutes(string expression) {
        Assert.NotEmpty(Scan($$"""
            class Legacy { string Url(string id) => {{expression}}; }
            """));
    }

    [Fact]
    public void Scanner_AllowsGeneratedClientDispatch() {
        const string source = "class Generated(HttpClient http) { void Call(string url) { _ = http.GetAsync(url); } }";
        Assert.Empty(Scan(source, "FoodDiary.Telegram.Bot/Api/Generated/Api/Generated.cs"));
        Assert.NotEmpty(Scan(source, "FoodDiary.Telegram.Bot/Operations/Generated/Generated.cs"));
    }

    [Fact]
    public void Scanner_AllowsExistingTransportAndStorageRequestShapes() {
        const string transport = """
            class BotApiTransport(HttpClient client, Uri baseUri) {
                HttpRequestMessage CreateRequest(string method, string path) => new HttpRequestMessage(new HttpMethod(method), new Uri(baseUri, path));
                Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            """;
        const string upload = """
            class BotDiaryClient(HttpClient client) {
                Task<HttpResponseMessage> UploadAsync(Uri uri, CancellationToken cancellationToken) {
                    var request = new HttpRequestMessage(HttpMethod.Put, uri);
                    return client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                }
            }
            """;
        Assert.Multiple(() => {
            Assert.Empty(Scan(transport, "FoodDiary.Telegram.Bot/Api/BotApiTransport.cs"));
            Assert.Empty(Scan(upload, "FoodDiary.Telegram.Bot/Operations/BotDiaryClient.cs"));
        });
    }

    [Theory]
    [InlineData("FoodDiary.Telegram.Bot/Api/BotApiTransport.cs", "BotApiTransport", "SendAsync")]
    [InlineData("FoodDiary.Telegram.Bot/Operations/BotDiaryClient.cs", "BotDiaryClient", "UploadAsync")]
    public void Scanner_RejectsExtraHttpCallsInsideTransportExceptions(string path, string type, string method) {
        Assert.NotEmpty(Scan($$"""
            class {{type}}(HttpClient client) { void {{method}}(string url) { _ = client.GetAsync(url); } }
            """, path));
    }

    [Fact]
    public void Scanner_AllowsGeneratedMethodCallsAndCheckpointSerialization() {
        Assert.Empty(Scan("""
            class BotApi { public void AcquireAsync() {} }
            class Worker { void Call() { new BotApi().AcquireAsync(); System.Text.Json.JsonSerializer.Serialize(new { checkpoint = "ready" }); } }
            """));
    }

    private static string[] Scan(string source, string path = "FoodDiary.Telegram.Bot/Legacy.cs") =>
        GeneratedApiClientUsageScanner.FindViolations([(path, source)]);

    [Fact]
    public void Scanner_RejectsCallingTheLowLevelApiTransportFromAnAdapter() {
        Assert.NotEmpty(Scan("""
            namespace FoodDiary.Telegram.Bot.Api;
            class BotApiTransport { public void CreateRequest(string path) {} }
            class Legacy { void Call(BotApiTransport transport, string path) { transport.CreateRequest(path); } }
            """));
    }
}
