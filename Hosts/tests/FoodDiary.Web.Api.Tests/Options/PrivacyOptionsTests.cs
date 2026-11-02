using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Web.Api.Tests.Options;

[ExcludeFromCodeCoverage]
public sealed class PrivacyOptionsTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HealthConsentEnforcement_BindsTheExplicitHostSetting(bool enabled) {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            [$"{PrivacyOptions.SectionName}:RequireHealthConsent"] = enabled ? "true" : "false",
        }).Build();

        PrivacyOptions? options = configuration.GetSection(PrivacyOptions.SectionName).Get<PrivacyOptions>();

        Assert.NotNull(options);
        Assert.Equal(enabled, options.RequireHealthConsent);
        Assert.True(new PrivacyOptions().RequireHealthConsent);
    }
}
