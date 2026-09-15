using FoodDiary.Application.Abstractions.Email.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdentityEmailOptionsTests {
    [Theory]
    [InlineData("Email:FrontendBaseUrl", "invalid")]
    [InlineData("Email:AllowedFrontendBaseUrls:0", "invalid")]
    [InlineData("Email:VerificationPath", "")]
    [InlineData("Email:PasswordResetPath", "")]
    public void InvalidOptions_AreRejectedWithoutCentralInfrastructure(string key, string value) {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { [key] = value }).Build();
        var services = new ServiceCollection();
        services.AddIdentityEmailOptions(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
    }

    [Fact]
    public void OptionsValue_IsTheSameInstanceAsDirectRegistration() {
        var services = new ServiceCollection();
        services.AddIdentityEmailOptions(new ConfigurationBuilder().Build());
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(provider.GetRequiredService<IOptions<EmailOptions>>().Value, provider.GetRequiredService<EmailOptions>());
    }
}
