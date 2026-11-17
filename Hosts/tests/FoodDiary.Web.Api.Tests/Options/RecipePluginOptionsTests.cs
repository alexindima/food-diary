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
    public void ProductionRejectsDuplicateClientIdOrNonLoopbackConfiguration(string clientId, string callbackUri) {
        IConfiguration configuration = BuildConfiguration(clientId, callbackUri);

        Assert.Throws<InvalidOperationException>(() => RecipePluginOptions.ReadEnabled(configuration, development: false));
    }

    private static IConfiguration BuildConfiguration(string clientId, string callbackUri) =>
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
