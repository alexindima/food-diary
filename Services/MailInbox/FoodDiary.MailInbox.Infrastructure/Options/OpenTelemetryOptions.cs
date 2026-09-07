namespace FoodDiary.MailInbox.Infrastructure.Options;

public sealed class OpenTelemetryOptions {
    public const string SectionName = "OpenTelemetry";

    public OtlpOptions Otlp { get; init; } = new();

    public static bool HasValidOtlpEndpoint(OpenTelemetryOptions options) =>
        TryCreateOtlpEndpoint(options.Otlp.Endpoint, out _);

    public static bool TryCreateOtlpEndpoint(string? endpoint, out Uri? endpointUri) {
        endpointUri = null;
        if (string.IsNullOrWhiteSpace(endpoint)) {
            return true;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? parsedUri) ||
            (!parsedUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !parsedUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
            !string.IsNullOrEmpty(parsedUri.UserInfo) ||
            !string.IsNullOrEmpty(parsedUri.Query) ||
            !string.IsNullOrEmpty(parsedUri.Fragment)) {
            return false;
        }

        endpointUri = parsedUri;
        return true;
    }

    public sealed class OtlpOptions {
        public string? Endpoint { get; init; }
    }
}
