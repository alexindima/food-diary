using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Web.Api.Tests.Options;

[ExcludeFromCodeCoverage]
public sealed class RecipePluginOptionsTests {
    [Fact]
    public void ProductionAcceptsExplicitNativeCodexClientWithLoopbackCallback() {
        IConfiguration configuration = BuildConfiguration("fooddiary-codex", "http://127.0.0.1:15473/callback");

        var options = RecipePluginOptions.ReadEnabled(configuration, development: false);

        Assert.NotNull(options);
        Assert.Equal("fooddiary-codex", options.CodexClientId);
        Assert.Equal(new Uri("http://127.0.0.1:15473/callback"), options.CodexRedirectUri);
    }

    [Theory]
    [InlineData("fooddiary-chatgpt", "http://127.0.0.1:15473/callback")]
    [InlineData("fooddiary-codex", "http://127.0.0.1/callback")]
    [InlineData("fooddiary-codex", "https://example.com:15473/callback")]
    [InlineData("fooddiary-codex", "http://127.0.0.1:15473/callback?forward=true")]
    [InlineData("fooddiary-codex", null)]
    [InlineData(null, "http://127.0.0.1:15473/callback")]
    public void ProductionRejectsDuplicateClientIdOrNonLoopbackConfiguration(string? clientId, string? callbackUri) {
        IConfiguration configuration = BuildConfiguration(clientId, callbackUri);

        Assert.Throws<InvalidOperationException>(() => RecipePluginOptions.ReadEnabled(configuration, development: false));
    }

    [Fact]
    public void NoRecipePluginSectionLeavesMcpDisabled() {
        IConfiguration config = new ConfigurationBuilder().Build();

        Assert.Null(RecipePluginOptions.ReadEnabled(config, development: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void MissingOrDisabledIntegrationDoesNotRequireOAuthConfiguration(string? enabled) {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["RecipePlugin:Enabled"] = enabled,
            ["RecipePlugin:Issuer"] = "https://example.com/unusable/path",
        }).Build();

        Assert.Null(RecipePluginOptions.ReadEnabled(config, development: false));
    }

    [Theory]
    [InlineData("Issuer", null)]
    [InlineData("Issuer", "https://mcp.fooddiary.club/extra")]
    [InlineData("Issuer", "http://127.0.0.1:5000/")]
    [InlineData("WebOrigin", "https://fooddiary.club/extra")]
    [InlineData("WebOrigin", "https://fooddiary.club/?x=1")]
    [InlineData("WebOrigin", "https://fooddiary.club/#fragment")]
    [InlineData("RedirectUri", "http://chatgpt.com/connector_platform_oauth_redirect")]
    [InlineData("RedirectUri", "https://callback@example.com/")]
    [InlineData("RedirectUri", "/callback")]
    [InlineData("ClientId", " ")]
    public void ProductionRejectsInvalidOAuthEndpointOrClient(string key, string? value) {
        IConfiguration config = Set(BuildConfiguration("fooddiary-codex", "http://127.0.0.1:15473/callback"), key, value);

        Assert.Throws<InvalidOperationException>(() => RecipePluginOptions.ReadEnabled(config, development: false));
    }

    [Theory]
    [InlineData("SigningCertificatePath")]
    [InlineData("EncryptionCertificatePath")]
    public void ProductionRequiresBothCertificates(string key) {
        IConfiguration config = Set(BuildConfiguration("fooddiary-codex", "http://127.0.0.1:15473/callback"), key, value: null);

        Assert.Throws<InvalidOperationException>(() => RecipePluginOptions.ReadEnabled(config, development: false));
    }

    [Fact]
    public void DevelopmentAcceptsLoopbackAndOptionalCodexConfiguration() {
        IConfiguration config = BuildConfiguration(clientId: null, callbackUri: null);
        Set(config, "Issuer", "http://127.0.0.1:5000/");
        Set(config, "WebOrigin", "http://localhost:4200/");
        Set(config, "RedirectUri", "http://127.0.0.1:15473/callback");
        Set(config, "SigningCertificatePath", value: null);
        Set(config, "EncryptionCertificatePath", value: null);

        RecipePluginOptions options = Assert.IsType<RecipePluginOptions>(RecipePluginOptions.ReadEnabled(config, development: true));
        Assert.Multiple(
            () => Assert.Equal(new Uri("http://127.0.0.1:5000/"), options.Issuer),
            () => Assert.Equal("fooddiary-chatgpt", options.ClientId),
            () => Assert.Null(options.CodexClientId),
            () => Assert.Null(options.CodexRedirectUri));
    }

    [Fact]
    public void DevelopmentRejectsInsecurePublicOrigin() {
        IConfiguration config = Set(BuildConfiguration(clientId: null, callbackUri: null), "Issuer", "http://mcp.fooddiary.club/");

        Assert.Throws<InvalidOperationException>(() => RecipePluginOptions.ReadEnabled(config, development: true));
    }

    private static IConfiguration Set(IConfiguration configuration, string key, string? value) {
        configuration[$"RecipePlugin:{key}"] = value;
        return configuration;
    }

    private static IConfiguration BuildConfiguration(string? clientId, string? callbackUri) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["RecipePlugin:Enabled"] = "true",
            ["RecipePlugin:Issuer"] = "https://mcp.fooddiary.club/",
            ["RecipePlugin:WebOrigin"] = "https://fooddiary.club/",
            ["RecipePlugin:RedirectUri"] = "https://chatgpt.com/connector_platform_oauth_redirect",
            ["RecipePlugin:SigningCertificatePath"] = "/test/signing.pfx",
            ["RecipePlugin:EncryptionCertificatePath"] = "/test/encryption.pfx",
            ["RecipePlugin:CodexClientId"] = clientId,
            ["RecipePlugin:CodexRedirectUri"] = callbackUri,
        }).Build();
}
