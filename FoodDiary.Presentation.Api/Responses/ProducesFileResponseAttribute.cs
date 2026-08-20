namespace FoodDiary.Presentation.Api.Responses;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ProducesFileResponseAttribute : Attribute {
    public ProducesFileResponseAttribute(params string[] contentTypes) {
        ArgumentNullException.ThrowIfNull(contentTypes);
        if (contentTypes.Length == 0) {
            throw new ArgumentException("At least one response content type is required.", nameof(contentTypes));
        }

        var uniqueContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string contentType in contentTypes) {
            if (string.IsNullOrWhiteSpace(contentType) ||
                !System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(
                    contentType,
                    out System.Net.Http.Headers.MediaTypeHeaderValue? parsedContentType) ||
                parsedContentType.Parameters.Count > 0 ||
                parsedContentType.MediaType!.Contains('*', StringComparison.Ordinal) ||
                !string.Equals(contentType, contentType.Trim(), StringComparison.Ordinal)) {
                throw new ArgumentException($"'{contentType}' is not a valid parameterless media type.", nameof(contentTypes));
            }

            if (!uniqueContentTypes.Add(contentType)) {
                throw new ArgumentException($"Duplicate response content type '{contentType}'.", nameof(contentTypes));
            }
        }

        ContentTypes = Array.AsReadOnly((string[])contentTypes.Clone());
    }

    public IReadOnlyList<string> ContentTypes { get; }
}
