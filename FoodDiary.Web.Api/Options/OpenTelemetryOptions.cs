namespace FoodDiary.Web.Api.Options;

public sealed class OpenTelemetryOptions {
    public const string SectionName = "OpenTelemetry";

    public OtlpOptions Otlp { get; init; } = new();

    public sealed class OtlpOptions {
        public string? Endpoint { get; init; }
    }
}
