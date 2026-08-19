using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Integrations.Http;

namespace FoodDiary.Integrations.Services.OpenAi;

internal static class OpenAiErrorMetadata {
    private const int MaximumDiagnosticTokenLength = 64;

    public static string Summarize(string responseBody) {
        if (string.IsNullOrWhiteSpace(responseBody)) {
            return "empty";
        }

        try {
            var root = JsonNode.Parse(
                responseBody,
                documentOptions: new JsonDocumentOptions {
                    MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
                });
            if (root?["error"] is not JsonNode error) {
                return "response_metadata_unavailable";
            }

            if (error is not JsonObject errorObject) {
                return "provider_error";
            }

            string? type = NormalizeDiagnosticToken(errorObject["type"]);
            string? code = NormalizeDiagnosticToken(errorObject["code"]);
            return (type, code) switch {
                (not null, not null) => $"type={type}, code={code}",
                (not null, null) => $"type={type}",
                (null, not null) => $"code={code}",
                _ => "provider_error",
            };
        } catch (JsonException) {
            return "response_metadata_unavailable";
        }
    }

    private static string? NormalizeDiagnosticToken(JsonNode? node) {
        if (node is not JsonValue value ||
            !value.TryGetValue(out string? text) ||
            string.IsNullOrWhiteSpace(text)) {
            return null;
        }

        string token = text.Trim();
        if (token.Length > MaximumDiagnosticTokenLength || token.Any(static character =>
                !char.IsLetterOrDigit(character) && character is not ('_' or '-' or '.'))) {
            return null;
        }

        return token;
    }
}
