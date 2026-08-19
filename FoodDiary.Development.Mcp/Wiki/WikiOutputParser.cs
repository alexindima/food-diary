using System.Text.RegularExpressions;
using System.Text.Json;

namespace FoodDiary.Development.Mcp.Wiki;

public static partial class WikiOutputParser {
    public static WikiCommandResult Parse(
        string command,
        string rawOutput,
        string repositoryRoot,
        string gitHead) {
        string[] lines = rawOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        JsonElement? structuredOutput = TryParseJson(rawOutput);
        string[] referencedPaths = structuredOutput is JsonElement json
            ? CollectRepositoryPaths(json)
            : CollectRepositoryPaths(lines);
        string[] requiredChecks = structuredOutput is JsonElement checkJson
            ? CollectCommandValues(checkJson)
            : CollectCommandValues(lines);
        string[] warnings = structuredOutput is JsonElement warningJson
            ? CollectWarnings(warningJson)
            : CollectWarningLines(lines);
        string[] scopePaths = structuredOutput is JsonElement scopeJson &&
            string.Equals(command, "trace", StringComparison.OrdinalIgnoreCase)
                ? CollectTraceScopePaths(scopeJson)
                : referencedPaths;

        return new WikiCommandResult(
            command,
            rawOutput,
            structuredOutput,
            repositoryRoot,
            gitHead,
            lines,
            referencedPaths,
            requiredChecks,
            warnings,
            scopePaths);
    }

    private static string[] CollectRepositoryPaths(JsonElement root) => [.. EnumerateProperties(root)
        .Where(item => IsPathProperty(item.Name))
        .SelectMany(item => EnumerateStringValues(item.Value))
        .Where(value => !string.IsNullOrWhiteSpace(value) && RepositoryPathRegex().IsMatch(value))
        .Select(value => value!.Replace('\\', '/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)];

    private static string[] CollectRepositoryPaths(IEnumerable<string> lines) => [.. lines
        .SelectMany(line => RepositoryPathRegex().Matches(line).Select(match => match.Value.Replace('\\', '/')))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)];

    private static string[] CollectCommandValues(JsonElement root) => [.. EnumerateProperties(root)
        .Where(item => item.Name.Equals("command", StringComparison.OrdinalIgnoreCase) ||
            item.Name.Equals("commands", StringComparison.OrdinalIgnoreCase))
        .SelectMany(item => EnumerateStringValues(item.Value))
        .Where(IsVerificationCommand)
        .Select(value => value!)
        .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static string[] CollectCommandValues(IEnumerable<string> lines) => [.. lines
        .Where(line => line.StartsWith("- ", StringComparison.Ordinal) && IsVerificationCommand(line))
        .Select(line => line[2..])
        .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static string[] CollectWarnings(JsonElement root) => [.. EnumerateNamedValues(root, name =>
            name.Equals("warning", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("warnings", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("issues", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("errors", StringComparison.OrdinalIgnoreCase))
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static string[] CollectWarningLines(IEnumerable<string> lines) => [.. lines
        .Where(line => line.Contains("stale", StringComparison.OrdinalIgnoreCase) ||
                       line.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                       line.Contains("failed", StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static string[] CollectTraceScopePaths(JsonElement root) {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        foreach (string property in new[] { "symbols", "consumers", "candidates" }) {
            if (!root.TryGetProperty(property, out JsonElement items) || items.ValueKind != JsonValueKind.Array) {
                continue;
            }
            foreach (JsonElement item in items.EnumerateArray()) {
                if (string.Equals(property, "candidates", StringComparison.Ordinal) &&
                    item.TryGetProperty("confidence", out JsonElement confidence) &&
                    confidence.ValueKind == JsonValueKind.String &&
                    string.Equals(confidence.GetString(), "low", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                foreach (string pathProperty in new[] { "path", "consumerPath", "declarationPath" }) {
                    if (item.TryGetProperty(pathProperty, out JsonElement value) &&
                        value.ValueKind == JsonValueKind.String &&
                        value.GetString() is string path &&
                        RepositoryPathRegex().IsMatch(path)) {
                        paths.Add(path.Replace('\\', '/'));
                    }
                }
            }
        }
        return [.. paths.Order(StringComparer.OrdinalIgnoreCase)];
    }

    private static IEnumerable<(string Name, JsonElement Value)> EnumerateProperties(JsonElement element) {
        if (element.ValueKind == JsonValueKind.Object) {
            foreach (JsonProperty property in element.EnumerateObject()) {
                yield return (property.Name, property.Value);
                foreach ((string Name, JsonElement Value) nested in EnumerateProperties(property.Value)) {
                    yield return nested;
                }
            }
        } else if (element.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement item in element.EnumerateArray()) {
                foreach ((string Name, JsonElement Value) nested in EnumerateProperties(item)) {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateNamedValues(
        JsonElement root,
        Func<string, bool> predicate) {
        foreach ((string name, JsonElement value) in EnumerateProperties(root).Where(item => predicate(item.Name))) {
            if (value.ValueKind == JsonValueKind.String) {
                yield return value.GetString()!;
            } else if (value.ValueKind == JsonValueKind.Array) {
                foreach (JsonElement item in value.EnumerateArray()) {
                    if (item.ValueKind == JsonValueKind.String) {
                        yield return item.GetString()!;
                    } else if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("message", out JsonElement message) &&
                        message.ValueKind == JsonValueKind.String) {
                        yield return message.GetString()!;
                    }
                }
            }
        }
    }

    private static IEnumerable<string?> EnumerateStringValues(JsonElement value) {
        if (value.ValueKind == JsonValueKind.String) {
            yield return value.GetString();
        } else if (value.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement item in value.EnumerateArray()) {
                foreach (string? nested in EnumerateStringValues(item)) {
                    yield return nested;
                }
            }
        } else if (value.ValueKind == JsonValueKind.Object) {
            foreach (JsonProperty property in value.EnumerateObject()) {
                if (property.Name.Equals("path", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("command", StringComparison.OrdinalIgnoreCase)) {
                    foreach (string? nested in EnumerateStringValues(property.Value)) {
                        yield return nested;
                    }
                }
            }
        }
    }

    private static bool IsPathProperty(string name) =>
        name.Equals("path", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("paths", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith("Path", StringComparison.OrdinalIgnoreCase);

    private static bool IsVerificationCommand(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.Contains("dotnet ", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("wiki.ps1", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("npm ", StringComparison.OrdinalIgnoreCase));

    private static JsonElement? TryParseJson(string rawOutput) {
        try {
            using var document = JsonDocument.Parse(rawOutput);
            return document.RootElement.Clone();
        } catch (JsonException) {
            return null;
        }
    }

    [GeneratedRegex(@"(?<![\w.-])(?:\.llm-wiki|docs|tests|FoodDiary[\w.-]*|MailInbox|MailRelay|Shared)[/\\][\w./\\-]+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex RepositoryPathRegex();
}
