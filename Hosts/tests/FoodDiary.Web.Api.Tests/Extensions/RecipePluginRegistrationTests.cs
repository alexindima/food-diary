using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class RecipePluginRegistrationTests {
    [Theory]
    [InlineData(true, "Missing OAuth signing certificate.")]
    [InlineData(false, "Missing OAuth encryption certificate.")]
    public async Task ExplicitDevelopmentCertificatesMustProvideBothKeys(bool signingMissing, string expectedMessage) {
        string path = Path.GetTempFileName();
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=FoodDiary-owned-fixture", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        try {
            await File.WriteAllBytesAsync(path, certificate.Export(X509ContentType.Pkcs12, "fixture-password"));
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["RecipePlugin:Enabled"] = "true",
                ["RecipePlugin:Issuer"] = "https://mcp.example.test/",
                ["RecipePlugin:WebOrigin"] = "https://app.example.test/",
                ["RecipePlugin:RedirectUri"] = "https://callback.example.test/",
                ["RecipePlugin:SigningCertificatePath"] = signingMissing ? null : path,
                ["RecipePlugin:EncryptionCertificatePath"] = signingMissing ? path : null,
                ["RecipePlugin:SigningCertificatePassword"] = "fixture-password",
                ["RecipePlugin:EncryptionCertificatePassword"] = "fixture-password",
            }).Build();
            IHostEnvironment environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(Environments.Development);

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddRecipePlugin(configuration, environment));

            Assert.Equal(expectedMessage, error.Message);
        } finally {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializingAnExistingClientRequiresTheRegisteredRedirectToMatch(bool registeredRedirectMatches) {
        IOpenIddictApplicationManager applications = Substitute.For<IOpenIddictApplicationManager>();
        object existing = new();
        applications.FindByClientIdAsync("owned-fixture-client", CancellationToken.None).Returns(existing);
        applications.ValidateRedirectUriAsync(existing, "https://callback.example.test/", CancellationToken.None).Returns(registeredRedirectMatches);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(applications);
        builder.Services.AddSingleton(new RecipePluginOptions {
            Enabled = true,
            ClientId = "owned-fixture-client",
            Issuer = new Uri("https://mcp.example.test/"),
            WebOrigin = new Uri("https://app.example.test/"),
            RedirectUri = new Uri("https://callback.example.test/"),
        });
        await using WebApplication app = builder.Build();

        if (registeredRedirectMatches) {
            await app.InitializeRecipePluginAsync(CancellationToken.None);
        } else {
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => app.InitializeRecipePluginAsync(CancellationToken.None));
            Assert.Equal("RecipePlugin callback differs from the registered OAuth client.", error.Message);
        }

        await applications.Received(1).ValidateRedirectUriAsync(existing, "https://callback.example.test/", CancellationToken.None);
        await applications.DidNotReceive().CreateAsync(Arg.Any<OpenIddictApplicationDescriptor>(), Arg.Any<CancellationToken>());
    }
}
