namespace FoodDiary.Web.Api.Options;

public sealed class OpenTelemetryOptions {
    public const string SectionName = "OpenTelemetry";

    public OtlpOptions Otlp { get; init; } = new();

    public static bool HasValidOtlpEndpoint(OpenTelemetryOptions options) {
        return string.IsNullOrWhiteSpace(options.Otlp.Endpoint) ||
               Uri.TryCreate(options.Otlp.Endpoint, UriKind.Absolute, out _);
    }

    public sealed class OtlpOptions {
        public string? Endpoint { get; init; }
    }
}
