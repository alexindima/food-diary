using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Http;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Primitives;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed partial class RecipePluginOAuthTests {
    [RequiresDockerFact]
    public async Task DiscoveryExposesTheTwoWriteToolsWithTheirPublishedContracts() {
        using HttpClient browser = CreateBrowser(WebOrigin, cookies: true);
        using HttpClient oauth = CreateBrowser(Origin, cookies: false);
        await RegisterBrowserAsync(browser);
        JsonElement grant = await ConnectAsync(browser, oauth, RecipePluginAccess.RecipeScope + " " + RecipePluginAccess.ShoppingListScope);
        string token = grant.GetProperty("access_token").GetString()!;

        using HttpRequestMessage list = CreateMcpRequest(token, "tools/list", new { _meta = McpClientMetadata() });
        using HttpResponseMessage response = await oauth.SendAsync(list);
        JsonElement result = await ReadMcpResultAsync(response);
        Assert.Collection(result.GetProperty("tools").EnumerateArray().OrderBy(tool => tool.GetProperty("name").GetString(), StringComparer.Ordinal),
            tool => AssertTool(tool, "create_shopping_list", "shopping-lists:create", ["items", "name"]),
            tool => AssertTool(tool, "save_private_recipe", "recipes:create", ["name", "servings", "steps"]));

        using HttpRequestMessage foreignOrigin = CreateMcpRequest(token, "tools/list", new { _meta = McpClientMetadata() });
        foreignOrigin.Headers.Add("Origin", "https://example.net");
        using HttpResponseMessage forbidden = await oauth.SendAsync(foreignOrigin);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        JsonElement unknown = await CallAsync(oauth, token, "unregistered_fooddiary_tool", new { });
        Assert.False(IsToolSuccess(unknown));
        Assert.Equal("This tool is unavailable.", ToolMessage(unknown));
    }

    [RequiresDockerFact]
    public async Task ParallelRecipeCallsReplayOneWriteAndIdenticalArgumentsAreIsolatedByOwner() {
        using HttpClient firstBrowser = CreateBrowser(WebOrigin, cookies: true);
        using HttpClient secondBrowser = CreateBrowser(WebOrigin, cookies: true);
        using HttpClient oauth = CreateBrowser(Origin, cookies: false);
        await RegisterBrowserAsync(firstBrowser);
        await RegisterBrowserAsync(secondBrowser);
        string firstToken = (await ConnectAsync(firstBrowser, oauth, RecipePluginAccess.RecipeScope)).GetProperty("access_token").GetString()!;
        string secondToken = (await ConnectAsync(secondBrowser, oauth, RecipePluginAccess.RecipeScope)).GetProperty("access_token").GetString()!;
        object arguments = RecipeArguments();

        JsonElement[] concurrent = await Task.WhenAll(
            CallAsync(oauth, firstToken, RecipePluginAccess.RecipeTool, arguments),
            CallAsync(oauth, firstToken, RecipePluginAccess.RecipeTool, arguments));
        Guid firstId = Assert.Single(concurrent.Where(IsToolSuccess).Select(SavedId).Distinct());
        foreach (JsonElement failure in concurrent.Where(result => !IsToolSuccess(result))) {
            Assert.Equal("FoodDiary is already processing this save. Retry with the same details.", ToolMessage(failure));
        }

        Guid retryId = SavedId(await CallAsync(oauth, firstToken, RecipePluginAccess.RecipeTool, arguments));
        Guid secondId = SavedId(await CallAsync(oauth, secondToken, RecipePluginAccess.RecipeTool, arguments));
        using HttpResponseMessage firstRecipe = await firstBrowser.GetAsync($"/api/v1/recipes/{firstId:D}");
        using HttpResponseMessage secondRecipe = await secondBrowser.GetAsync($"/api/v1/recipes/{secondId:D}");
        using HttpResponseMessage otherRecipe = await secondBrowser.GetAsync($"/api/v1/recipes/{firstId:D}");
        Assert.Multiple(
            () => Assert.Equal(firstId, retryId),
            () => Assert.NotEqual(firstId, secondId),
            () => Assert.Equal(HttpStatusCode.OK, firstRecipe.StatusCode),
            () => Assert.Equal(HttpStatusCode.OK, secondRecipe.StatusCode),
            () => Assert.Contains(otherRecipe.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden }));
    }

    [RequiresDockerFact]
    public async Task RequestPendingAfterDurableSaveCannotWriteAgain() {
        var store = new GatedCompletionStore();
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<IIdempotencyStore>(store))));
        using HttpClient browser = app.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false, HandleCookies = true, BaseAddress = WebOrigin,
        });
        using HttpClient oauth = app.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false, HandleCookies = false, BaseAddress = Origin,
        });
        await RegisterBrowserAsync(browser);
        string token = (await ConnectAsync(browser, oauth, RecipePluginAccess.RecipeScope)).GetProperty("access_token").GetString()!;
        object arguments = RecipeArguments();

        Task<JsonElement> first = CallAsync(oauth, token, RecipePluginAccess.RecipeTool, arguments);
        try {
            Guid savedBeforeReply = await store.Saved.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System);
            using HttpResponseMessage savedRecipe = await browser.GetAsync($"/api/v1/recipes/{savedBeforeReply:D}");
            Assert.Equal(HttpStatusCode.OK, savedRecipe.StatusCode);

            JsonElement duplicate = await CallAsync(oauth, token, RecipePluginAccess.RecipeTool, arguments);
            Assert.False(IsToolSuccess(duplicate));
            Assert.Equal("FoodDiary is already processing this save. Retry with the same details.", ToolMessage(duplicate));
        } finally {
            store.AllowCompletion();
            await first;
        }

        Guid originalId = SavedId(await first);
        Assert.Equal(originalId, SavedId(await CallAsync(oauth, token, RecipePluginAccess.RecipeTool, arguments)));
    }

    [RequiresDockerFact]
    public async Task ThrownToolFailureReleasesTheReservationForAnIdenticalRetry() {
        int failurePending = 1;
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Scoped<ISender>(provider => {
                IMediator mediator = provider.GetRequiredService<IMediator>();
                if (provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Path != RecipePluginAccess.ResourcePath) {
                    return mediator;
                }
                return new FailOnceSender(mediator, () => Interlocked.Exchange(ref failurePending, 0) == 1);
            }))));
        using HttpClient browser = app.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false, HandleCookies = true, BaseAddress = WebOrigin,
        });
        using HttpClient oauth = app.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false, HandleCookies = false, BaseAddress = Origin,
        });
        await RegisterBrowserAsync(browser);
        JsonElement grant = await ConnectAsync(browser, oauth, RecipePluginAccess.RecipeScope);
        string token = grant.GetProperty("access_token").GetString()!;
        object arguments = RecipeArguments();

        JsonElement failure = await CallAsync(oauth, token, RecipePluginAccess.RecipeTool, arguments);
        Assert.False(IsToolSuccess(failure));
        Assert.Equal(0, failurePending);

        Guid id = SavedId(await CallAsync(oauth, token, RecipePluginAccess.RecipeTool, arguments));
        using HttpResponseMessage saved = await browser.GetAsync($"/api/v1/recipes/{id:D}");
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
    }

    [RequiresDockerFact]
    public async Task ToolValidationErrorsKeepTheWireErrorAndAllowLaterValidRequests() {
        using HttpClient browser = CreateBrowser(WebOrigin, cookies: true);
        using HttpClient oauth = CreateBrowser(Origin, cookies: false);
        await RegisterBrowserAsync(browser);
        JsonElement grant = await ConnectAsync(browser, oauth, RecipePluginAccess.RecipeScope + " " + RecipePluginAccess.ShoppingListScope);
        string token = grant.GetProperty("access_token").GetString()!;

        await AssertRejectedAsync(RecipePluginAccess.RecipeTool, RecipeArguments(), "servings", JsonValue.Create(0),
            "Provide a recipe name, servings, and cooking steps with valid ingredient names.");
        await AssertRejectedAsync(RecipePluginAccess.ShoppingListTool, ShoppingListArguments(), "items", new JsonArray(),
            "Provide a list name and shopping items with valid names.");
        await AssertRejectedAsync(RecipePluginAccess.RecipeTool, RecipeArguments(), "steps", JsonNode.Parse("[null]"),
            "Provide a recipe name, servings, and cooking steps with valid ingredient names.");
        await AssertRejectedAsync(RecipePluginAccess.RecipeTool, RecipeArguments(), "steps",
            JsonNode.Parse("""[{"description":"Cook.","ingredients":[null]}]"""),
            "Provide a recipe name, servings, and cooking steps with valid ingredient names.");
        await AssertRejectedAsync(RecipePluginAccess.ShoppingListTool, ShoppingListArguments(), "items", JsonNode.Parse("[null]"),
            "Provide a list name and shopping items with valid names.");
        await AssertRejectedAsync(RecipePluginAccess.RecipeTool, RecipeArguments(), "name", JsonValue.Create(new string('x', 257)),
            "FoodDiary could not save this recipe. Check the name, portions, steps, and ingredient details.");
        await AssertRejectedAsync(RecipePluginAccess.ShoppingListTool, ShoppingListArguments(), "name", JsonValue.Create(new string('x', 129)),
            "FoodDiary could not save this list. Check the list and item details.");

        Assert.NotEqual(Guid.Empty, SavedId(await CallAsync(oauth, token, RecipePluginAccess.RecipeTool, RecipeArguments())));
        Assert.NotEqual(Guid.Empty, SavedId(await CallAsync(oauth, token, RecipePluginAccess.ShoppingListTool, ShoppingListArguments())));

        async Task AssertRejectedAsync(string tool, object valid, string property, JsonNode? value, string message) {
            var arguments = (JsonObject)JsonSerializer.SerializeToNode(valid)!;
            arguments[property] = value;
            JsonElement first = await CallAsync(oauth, token, tool, arguments);
            JsonElement retry = await CallAsync(oauth, token, tool, arguments);
            Assert.Multiple(
                () => Assert.False(IsToolSuccess(first)),
                () => Assert.Equal(message, ToolMessage(first)),
                () => Assert.False(IsToolSuccess(retry)),
                () => Assert.Equal(message, ToolMessage(retry)));
        }
    }

    [RequiresDockerFact]
    public async Task SignedOutConsentShowsSignInAndCancelReturnsNoAuthorizationCode() {
        using HttpClient browser = CreateBrowser(WebOrigin, cookies: true);
        (Dictionary<string, string> parameters, _) = CreateAuthorization(RecipePluginAccess.RecipeScope);
        string url = AuthorizationUrl(parameters);
        using HttpResponseMessage signIn = await browser.GetAsync(url);
        string signInPage = await signIn.Content.ReadAsStringAsync();
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, signIn.StatusCode),
            () => Assert.Contains("Sign in to FoodDiary", signInPage, StringComparison.Ordinal),
            () => Assert.DoesNotContain("__RequestVerificationToken", signInPage, StringComparison.Ordinal));

        await RegisterBrowserAsync(browser);
        browser.DefaultRequestHeaders.AcceptLanguage.Clear();
        browser.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
        using HttpResponseMessage consent = await browser.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
        string page = await consent.Content.ReadAsStringAsync();
        Assert.Contains("Save private recipes", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Create shopping lists", page, StringComparison.Ordinal);
        Match token = FormToken().Match(page);
        Assert.True(token.Success);
        parameters["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups["token"].Value);
        parameters["fooddiary_decision"] = "deny";
        using var form = new FormUrlEncodedContent(parameters);
        using HttpResponseMessage denied = await browser.PostAsync(RecipePluginAccess.AuthorizationPath, form);
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Uri redirect = Assert.IsType<Uri>(denied.Headers.Location, exactMatch: false);
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(redirect.Query);
        Assert.Multiple(
            () => Assert.Equal(Callback, redirect.GetLeftPart(UriPartial.Path)),
            () => Assert.Equal(parameters["state"], query["state"].ToString()),
            () => Assert.Equal("access_denied", query["error"].ToString()),
            () => Assert.False(query.ContainsKey("code")));
    }

    [RequiresDockerFact]
    public async Task BrowserLogoutInvalidatesBothDelegatedAccessAndRefresh() {
        using HttpClient browser = CreateBrowser(WebOrigin, cookies: true);
        using HttpClient oauth = CreateBrowser(Origin, cookies: false);
        await RegisterBrowserAsync(browser);
        JsonElement grant = await ConnectAsync(browser, oauth, RecipePluginAccess.RecipeScope + " offline_access");
        string access = grant.GetProperty("access_token").GetString()!;
        string refresh = grant.GetProperty("refresh_token").GetString()!;
        using HttpRequestMessage list = CreateMcpRequest(access, "tools/list", new { _meta = McpClientMetadata() });
        using HttpResponseMessage before = await oauth.SendAsync(list);
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout.Headers.Add("X-Requested-With", "XMLHttpRequest");
        using HttpResponseMessage loggedOut = await browser.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);
        using HttpResponseMessage denied = await SendMcpAsync(oauth, access, RecipePluginAccess.RecipeTool, RecipeArguments());
        using var refreshForm = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
            ["client_id"] = ClientId,
            ["refresh_token"] = refresh,
            ["resource"] = Resource.AbsoluteUri,
            ["grant_type"] = "refresh_token",
        });
        using HttpResponseMessage refreshing = await oauth.PostAsync(RecipePluginAccess.TokenPath, refreshForm);
        JsonElement error = await refreshing.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode),
            () => Assert.Equal(HttpStatusCode.BadRequest, refreshing.StatusCode),
            () => Assert.Equal("invalid_grant", error.GetProperty("error").GetString()));
    }

    private HttpClient CreateBrowser(Uri origin, bool cookies) => factory.CreateClient(new WebApplicationFactoryClientOptions {
        AllowAutoRedirect = false, HandleCookies = cookies, BaseAddress = origin,
    });

    private static async Task RegisterBrowserAsync(HttpClient browser) {
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register", new {
            email = $"mcp-contract-{Guid.NewGuid():N}@example.com",
            password = "Password123!",
            language = "ru",
        });
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        browser.DefaultRequestHeaders.Remove("X-Requested-With");
        browser.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru");
    }

    private static (Dictionary<string, string> Parameters, string Verifier) CreateAuthorization(
        string scopes, string clientId = ClientId, string callback = Callback) {
        string verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (new Dictionary<string, string>(StringComparer.Ordinal) {
            ["client_id"] = clientId,
            ["redirect_uri"] = callback,
            ["response_type"] = "code",
            ["resource"] = Resource.AbsoluteUri,
            ["scope"] = scopes,
            ["state"] = Guid.NewGuid().ToString("N"),
            ["code_challenge"] = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
            ["code_challenge_method"] = "S256",
        }, verifier);
    }

    private static string AuthorizationUrl(Dictionary<string, string> parameters) =>
        QueryHelpers.AddQueryString(RecipePluginAccess.AuthorizationPath,
            parameters.Select(static parameter => new KeyValuePair<string, string?>(parameter.Key, parameter.Value)));

    private static HttpRequestMessage CreateMcpRequest(string token, string method, object parameters) {
        var request = new HttpRequestMessage(HttpMethod.Post, RecipePluginAccess.ResourcePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", ProtocolVersion);
        request.Headers.Add("Mcp-Method", method);
        request.Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method, @params = parameters });
        return request;
    }

    private static Dictionary<string, object> McpClientMetadata() => new(StringComparer.Ordinal) {
        ["io.modelcontextprotocol/protocolVersion"] = ProtocolVersion,
        ["io.modelcontextprotocol/clientInfo"] = new { name = "FoodDiary integration test", version = "1.0" },
        ["io.modelcontextprotocol/clientCapabilities"] = new { },
    };

    private static void AssertTool(JsonElement tool, string name, string scope, string[] required) {
        JsonElement annotations = tool.GetProperty("annotations");
        JsonElement inputSchema = tool.GetProperty("inputSchema");
        JsonElement security = Assert.Single(tool.GetProperty("_meta").GetProperty("securitySchemes").EnumerateArray());
        Assert.Multiple(
            () => Assert.Equal(name, tool.GetProperty("name").GetString()),
            () => Assert.False(annotations.GetProperty("readOnlyHint").GetBoolean()),
            () => Assert.False(annotations.GetProperty("destructiveHint").GetBoolean()),
            () => Assert.False(annotations.GetProperty("openWorldHint").GetBoolean()),
            () => Assert.Equal("object", inputSchema.GetProperty("type").GetString()),
            () => Assert.Equal(required, inputSchema.GetProperty("required").EnumerateArray()
                .Select(value => value.GetString()).Order(StringComparer.Ordinal).ToArray()),
            () => Assert.Equal("oauth2", security.GetProperty("type").GetString()),
            () => Assert.Equal(scope, Assert.Single(security.GetProperty("scopes").EnumerateArray()).GetString()));
    }

    private static bool IsToolSuccess(JsonElement result) =>
        !result.TryGetProperty("isError", out JsonElement error) || !error.GetBoolean();

    private static string? ToolMessage(JsonElement result) =>
        Assert.Single(result.GetProperty("content").EnumerateArray()).GetProperty("text").GetString();

    private static Guid SavedId(JsonElement result) => result.GetProperty("structuredContent").GetProperty("id").GetGuid();

    [ExcludeFromCodeCoverage]
    private sealed class FailOnceSender(ISender inner, Func<bool> shouldFail) : ISender {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            shouldFail() ? Task.FromException<TResponse>(new InvalidOperationException("Synthetic write failure.")) :
                inner.Send(request, cancellationToken);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            inner.Send(request, cancellationToken);

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            inner.Send(request, cancellationToken);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => inner.CreateStream(request, cancellationToken);

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            inner.CreateStream(request, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class GatedCompletionStore : IIdempotencyStore {
        private readonly InMemoryIdempotencyStore _inner = new(TimeProvider.System);
        private readonly TaskCompletionSource<Guid> _saved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _continue = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Guid> Saved => _saved.Task;
        public void AllowCompletion() => _continue.TrySetResult();

        public Task<IdempotencyReservation> ReserveAsync(string key, string requestHash, TimeSpan responseTtl,
            TimeSpan processingTtl, CancellationToken cancellationToken = default) =>
            _inner.ReserveAsync(key, requestHash, responseTtl, processingTtl, cancellationToken);

        public Task<bool> RenewAsync(string key, string requestHash, string ownerToken,
            TimeSpan processingTtl, CancellationToken cancellationToken = default) =>
            _inner.RenewAsync(key, requestHash, ownerToken, processingTtl, cancellationToken);

        public async Task<bool> CompleteAsync(string key, string requestHash, string ownerToken,
            int statusCode, string? body, string? location, TimeSpan responseTtl, CancellationToken cancellationToken = default) {
            if (key.StartsWith("recipe-plugin:", StringComparison.Ordinal) && body is not null) {
                using var saved = JsonDocument.Parse(body);
                _saved.TrySetResult(SavedId(saved.RootElement));
                await _continue.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System, cancellationToken);
            }
            return await _inner.CompleteAsync(key, requestHash, ownerToken, statusCode, body, location, responseTtl, cancellationToken);
        }

        public Task ReleaseAsync(string key, string requestHash, string ownerToken, CancellationToken cancellationToken = default) =>
            _inner.ReleaseAsync(key, requestHash, ownerToken, cancellationToken);
    }

}
