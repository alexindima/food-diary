using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using OpenIddict.Validation;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed partial class RecipePluginOAuthTests(RecipePluginOAuthTests.PluginFactory factory)
    : IClassFixture<RecipePluginOAuthTests.PluginFactory> {
    private const string Callback = "http://127.0.0.1:38105/callback";
    private const string ClientId = "fooddiary-integration-mcp";
    private const string CodexClientId = "fooddiary-integration-codex";
    private const string CodexCallback = "http://127.0.0.1:38106/callback";
    private const string ProtocolVersion = "2026-07-28";
    private static readonly Uri Origin = new("http://127.0.0.1:38120");
    private static readonly Uri WebOrigin = new("http://localhost:38121");
    private static readonly Uri Resource = new(Origin, RecipePluginAccess.ResourcePath);

    [RequiresDockerFact]
    public async Task BrowserConsentGrantsOnlyRequestedWritesAndRevocationStopsAccess() {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = WebOrigin,
        });
        using HttpClient oauth = factory.CreateClient(new WebApplicationFactoryClientOptions {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = Origin,
        });
        using HttpResponseMessage challenge = await oauth.PostAsJsonAsync(Resource, new { });
        Assert.Equal(HttpStatusCode.Unauthorized, challenge.StatusCode);
        Assert.Contains(RecipePluginAccess.ResourceMetadataPath, challenge.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
        JsonElement metadata = await oauth.GetFromJsonAsync<JsonElement>(RecipePluginAccess.ResourceMetadataPath);
        JsonElement discovery = await oauth.GetFromJsonAsync<JsonElement>("/.well-known/oauth-authorization-server");
        Assert.Multiple(
            () => Assert.Equal(Resource.AbsoluteUri, metadata.GetProperty("resource").GetString()),
            () => Assert.Equal(discovery.GetProperty("issuer").GetString(), metadata.GetProperty("authorization_servers")[0].GetString()),
            () => Assert.Equal(new Uri(WebOrigin, RecipePluginAccess.AuthorizationPath).AbsoluteUri, discovery.GetProperty("authorization_endpoint").GetString()),
            () => Assert.Equal(new Uri(Origin, RecipePluginAccess.TokenPath).AbsoluteUri, discovery.GetProperty("token_endpoint").GetString()),
            () => Assert.Equal("S256", Assert.Single(discovery.GetProperty("code_challenge_methods_supported").EnumerateArray()).GetString()),
            () => Assert.DoesNotContain(discovery.GetProperty("scopes_supported").EnumerateArray(), entry => entry.GetString() is "openid"),
            () => Assert.Contains(discovery.GetProperty("token_endpoint_auth_methods_supported").EnumerateArray(), entry => entry.GetString() is "none"),
            () => Assert.True(discovery.GetProperty("authorization_response_iss_parameter_supported").GetBoolean()));

        using HttpResponseMessage registration = await browser.PostAsJsonAsync("/api/v1/auth/register", new {
            email = $"recipe-plugin-{Guid.NewGuid():N}@example.com",
            password = "Password123!",
            language = "ru",
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        browser.DefaultRequestHeaders.Remove("X-Requested-With");
        browser.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru");

        JsonElement recipeGrant = await ConnectAsync(browser, oauth, RecipePluginAccess.RecipeScope);
        string recipeToken = recipeGrant.GetProperty("access_token").GetString()!;
        JsonElement rejected = await CallAsync(oauth, recipeToken, RecipePluginAccess.ShoppingListTool, ShoppingListArguments());
        Assert.True(rejected.GetProperty("isError").GetBoolean());
        Assert.Contains("shopping-lists:create", rejected.GetProperty("_meta").GetRawText(), StringComparison.Ordinal);
        JsonElement saveRecipe = await CallAsync(oauth, recipeToken, RecipePluginAccess.RecipeTool, RecipeArguments());
        Guid recipeId = saveRecipe.GetProperty("structuredContent").GetProperty("id").GetGuid();
        Assert.Equal(new Uri(WebOrigin, $"/recipes/{recipeId:D}/edit").AbsoluteUri,
            saveRecipe.GetProperty("structuredContent").GetProperty("editUrl").GetString());
        JsonElement retry = await CallAsync(oauth, recipeToken, RecipePluginAccess.RecipeTool, RecipeArguments());
        Assert.Equal(recipeId, retry.GetProperty("structuredContent").GetProperty("id").GetGuid());

        const string bothScopes = RecipePluginAccess.RecipeScope + " " + RecipePluginAccess.ShoppingListScope + " offline_access";
        JsonElement bothGrant = await ConnectAsync(browser, oauth, bothScopes, browserNavigation: true, CodexClientId, CodexCallback);
        string bothToken = await RefreshAsync(oauth, bothGrant.GetProperty("refresh_token").GetString()!, CodexClientId);
        JsonElement saveList = await CallAsync(oauth, bothToken, RecipePluginAccess.ShoppingListTool, ShoppingListArguments());
        Guid listId = saveList.GetProperty("structuredContent").GetProperty("id").GetGuid();
        Assert.Equal(new Uri(WebOrigin, "/shopping-lists").AbsoluteUri,
            saveList.GetProperty("structuredContent").GetProperty("shoppingListsUrl").GetString());
        JsonElement savedRecipe = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/recipes/{recipeId:D}");
        JsonElement savedList = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/shopping-lists/{listId:D}");
        Assert.Multiple(
            () => Assert.Equal("Private", savedRecipe.GetProperty("visibility").GetString()),
            () => Assert.Equal("Beans", savedRecipe.GetProperty("steps")[0].GetProperty("ingredients")[0].GetProperty("textName").GetString()),
            () => Assert.Equal("250 g", savedList.GetProperty("items")[0].GetProperty("note").GetString()));

        using var generalApi = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/info");
        generalApi.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bothToken);
        using HttpResponseMessage generalApiResponse = await browser.SendAsync(generalApi);
        Assert.Equal(HttpStatusCode.Unauthorized, generalApiResponse.StatusCode);

        using var revokeBody = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
            ["client_id"] = CodexClientId,
            ["token"] = bothToken,
        });
        using HttpResponseMessage revoked = await oauth.PostAsync(RecipePluginAccess.RevocationPath, revokeBody);
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        using HttpResponseMessage afterRevoke = await SendMcpAsync(oauth, bothToken, RecipePluginAccess.ShoppingListTool, ShoppingListArguments());
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);

        DateTimeOffset originalTime = factory.ValidationClock.UtcNow;
        try {
            factory.ValidationClock.UtcNow += TimeSpan.FromMinutes(6);
            using HttpResponseMessage expired = await SendMcpAsync(oauth, recipeToken, RecipePluginAccess.RecipeTool, RecipeArguments());
            Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        } finally {
            factory.ValidationClock.UtcNow = originalTime;
        }
    }

    private static object RecipeArguments() => new {
        name = "Beans for dinner",
        servings = 2,
        steps = new[] { new { description = "Warm the beans.", ingredients = new[] { new { name = "Beans", amountText = "250 g" } } } },
    };

    private static object ShoppingListArguments() => new {
        name = "Dinner shopping",
        items = new[] { new { name = "Beans", amountText = "250 g" } },
    };

    private static async Task<JsonElement> ConnectAsync(HttpClient browser, HttpClient oauth, string scopes,
        bool browserNavigation = false, string clientId = ClientId, string callback = Callback) {
        (Dictionary<string, string> parameters, string verifier) = CreateAuthorization(scopes, clientId, callback);
        using HttpResponseMessage consent = await browser.GetAsync(AuthorizationUrl(parameters));
        string page = await consent.Content.ReadAsStringAsync();
        Assert.True(consent.StatusCode == HttpStatusCode.OK, $"Consent page returned {consent.StatusCode}: {page}");
        Assert.Equal("strict-origin", Assert.Single(consent.Headers.GetValues("Referrer-Policy")));
        int styleStart = page.IndexOf("<style>", StringComparison.Ordinal) + "<style>".Length;
        int styleEnd = page.IndexOf("</style>", styleStart, StringComparison.Ordinal);
        Assert.True(styleStart >= "<style>".Length && styleEnd > styleStart);
        string styleHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(page[styleStart..styleEnd])));
        Assert.Equal($"default-src 'none'; img-src 'self'; style-src 'sha256-{styleHash}'; form-action 'self'; frame-ancestors 'none'",
            Assert.Single(consent.Headers.GetValues("Content-Security-Policy")));
        Assert.Contains("Сохранять личные рецепты", page, StringComparison.Ordinal);
        if (scopes.Contains(RecipePluginAccess.ShoppingListScope, StringComparison.Ordinal)) {
            Assert.Contains("Создавать списки покупок", page, StringComparison.Ordinal);
        } else {
            Assert.DoesNotContain("Создавать списки покупок", page, StringComparison.Ordinal);
        }
        Match antiforgery = FormToken().Match(page);
        Assert.True(antiforgery.Success);
        parameters["fooddiary_decision"] = "approve";
        using (var missingProof = new FormUrlEncodedContent(parameters))
        using (HttpResponseMessage missing = await browser.PostAsync(RecipePluginAccess.AuthorizationPath, missingProof)) {
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        }
        parameters["__RequestVerificationToken"] = WebUtility.HtmlDecode(antiforgery.Groups["token"].Value);
        using var approval = new HttpRequestMessage(HttpMethod.Post, RecipePluginAccess.AuthorizationPath) {
            Content = new FormUrlEncodedContent(parameters),
        };
        if (browserNavigation) {
            approval.Headers.Add("Sec-Fetch-Mode", "navigate");
        }
        using HttpResponseMessage approved = await browser.SendAsync(approval);
        Assert.Equal(browserNavigation ? HttpStatusCode.OK : HttpStatusCode.Redirect, approved.StatusCode);
        Uri redirect = browserNavigation
            ? await ReadContinuationAsync(approved)
            : Assert.IsType<Uri>(approved.Headers.Location, exactMatch: false);
        Assert.Equal(callback, redirect.GetLeftPart(UriPartial.Path));
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(redirect.Query);
        Assert.Equal(parameters["state"], query["state"].ToString());
        Assert.Equal(Origin.AbsoluteUri, query["iss"].ToString());
        string code = Assert.Single(query["code"])!;
        var exchange = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["client_id"] = clientId,
            ["code"] = code,
            ["redirect_uri"] = callback,
            ["resource"] = Resource.AbsoluteUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
        };
        using (var wrongProof = new FormUrlEncodedContent(exchange))
        using (HttpResponseMessage rejected = await oauth.PostAsync(RecipePluginAccess.TokenPath, wrongProof)) {
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
        exchange["code_verifier"] = verifier;
        using var form = new FormUrlEncodedContent(exchange);
        using HttpResponseMessage token = await oauth.PostAsync(RecipePluginAccess.TokenPath, form);
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);
        return await token.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<Uri> ReadContinuationAsync(HttpResponseMessage approved) {
        Assert.Null(approved.Headers.Location);
        Assert.Equal("no-referrer", Assert.Single(approved.Headers.GetValues("Referrer-Policy")));
        string page = await approved.Content.ReadAsStringAsync();
        Match link = ContinuationLink().Match(page);
        Assert.True(link.Success);
        string encodedUrl = link.Groups["url"].Value;
        Assert.Contains($"http-equiv=\"refresh\" content=\"0;url={encodedUrl}\"", page, StringComparison.Ordinal);
        return new Uri(WebUtility.HtmlDecode(encodedUrl));
    }

    private static async Task<string> RefreshAsync(HttpClient oauth, string refreshToken, string clientId) {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
            ["client_id"] = clientId,
            ["refresh_token"] = refreshToken,
            ["resource"] = Resource.AbsoluteUri,
            ["grant_type"] = "refresh_token",
        });
        using HttpResponseMessage response = await oauth.PostAsync(RecipePluginAccess.TokenPath, form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("access_token").GetString()!;
    }

    private static async Task<JsonElement> CallAsync(HttpClient browser, string token, string tool, object arguments) {
        using HttpResponseMessage response = await SendMcpAsync(browser, token, tool, arguments);
        return await ReadMcpResultAsync(response);
    }

    private static async Task<JsonElement> ReadMcpResultAsync(HttpResponseMessage response) {
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"MCP call returned {response.StatusCode}: {body}");
        string json = response.Content.Headers.ContentType?.MediaType is "text/event-stream"
            ? body.Split('\n').Single(line => line.StartsWith("data: ", StringComparison.Ordinal))[6..]
            : body;
        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("result", out JsonElement result), json);
        return result.Clone();
    }

    private static async Task<HttpResponseMessage> SendMcpAsync(HttpClient client, string token, string tool, object arguments) {
        using HttpRequestMessage request = CreateMcpRequest(token, "tools/call", new { name = tool, arguments, _meta = McpClientMetadata() });
        request.Headers.Add("Mcp-Name", tool);
        return await client.SendAsync(request);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"(?<token>[^\"]+)\"", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex FormToken();

    [GeneratedRegex("""<a [^>]*\brel="noreferrer" href="(?<url>[^"]+)">""", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ContinuationLink();

    [ExcludeFromCodeCoverage]
    public sealed class PluginFactory : PostgresApiWebApplicationFactory {
        public CookieExpiryApiWebApplicationFactory.ControlledClock ValidationClock { get; } = new();

        protected override async Task PrepareDatabaseAsync(string connectionString) {
            DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>().UseNpgsql(connectionString).Options;
            await using var context = new FoodDiaryDbContext(options);
            await context.Database.MigrateAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            base.ConfigureWebHost(builder);
            builder.UseSetting("RecipePlugin:Enabled", "true");
            builder.UseSetting("RecipePlugin:Issuer", Origin.AbsoluteUri);
            builder.UseSetting("RecipePlugin:WebOrigin", WebOrigin.AbsoluteUri);
            builder.UseSetting("RecipePlugin:ClientId", ClientId);
            builder.UseSetting("RecipePlugin:RedirectUri", Callback);
            builder.UseSetting("RecipePlugin:CodexClientId", CodexClientId);
            builder.UseSetting("RecipePlugin:CodexRedirectUri", CodexCallback);
            builder.ConfigureServices(services =>
                services.Configure<OpenIddictValidationOptions>(options => options.TimeProvider = ValidationClock));
        }
    }
}
