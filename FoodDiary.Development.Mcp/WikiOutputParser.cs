using System.Text.RegularExpressions;

namespace FoodDiary.Development.Mcp;

public static partial class WikiOutputParser {
    public static WikiCommandResult Parse(
        string command,
        string rawOutput,
        string repositoryRoot,
        string gitHead) {
        string[] lines = rawOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] referencedPaths = [.. lines
            .SelectMany(line => RepositoryPathRegex().Matches(line).Select(match => match.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
        string[] requiredChecks = [.. lines
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal) &&
                           (line.Contains("dotnet ", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("wiki.ps1", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("npm ", StringComparison.OrdinalIgnoreCase)))
            .Select(line => line[2..])
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        string[] warnings = [.. lines
            .Where(line => line.Contains("stale", StringComparison.OrdinalIgnoreCase) ||
                           line.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                           line.Contains("failed", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        return new WikiCommandResult(
            command,
            rawOutput,
            repositoryRoot,
            gitHead,
            lines,
            referencedPaths,
            requiredChecks,
            warnings);
    }

    [GeneratedRegex(@"(?<![\w.-])(?:\.llm-wiki|docs|tests|FoodDiary[\w.-]*|MailInbox|MailRelay|Shared)[/\\][\w./\\-]+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex RepositoryPathRegex();
}
