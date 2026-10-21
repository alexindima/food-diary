using FoodDiary.Web.Api.Options;

namespace FoodDiary.Web.Api.Tests.Options;

[ExcludeFromCodeCoverage]
public sealed class OpenTelemetryEndpointOptionsTests {
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("http://localhost:4317", true)]
    [InlineData("not-a-uri", false)]
    public void ExplicitEndpoint_UsesTheSameOptionalAbsoluteUriValidationAsBoundConfiguration(string? endpoint, bool expectedValid) {
        var options = new OpenTelemetryOptions { Otlp = new OpenTelemetryOptions.OtlpOptions { Endpoint = endpoint } };

        bool valid = OpenTelemetryOptions.HasValidOtlpEndpoint(options);

        Assert.Multiple(
            () => Assert.Equal(endpoint, options.Otlp.Endpoint),
            () => Assert.Equal(expectedValid, valid));
    }
}
