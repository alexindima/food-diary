using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Infrastructure;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class RepositoryRootResolver {
    public const string RepositoryRootEnvironmentVariable = "FOODDIARY_REPOSITORY_ROOT";

    public static string Resolve() {
        string? configuredRoot = Environment.GetEnvironmentVariable(RepositoryRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredRoot)) {
            return Validate(configuredRoot);
        }

        DirectoryInfo? directory = new(Environment.CurrentDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, ".llm-wiki", "wiki.ps1"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DevelopmentMcpException(
            DevelopmentMcpErrorCodes.RepositoryNotFound,
            $"FoodDiary repository root was not found. Set {RepositoryRootEnvironmentVariable}.");
    }

    private static string Validate(string candidate) {
        string fullPath = Path.GetFullPath(candidate);
        string wikiPath = Path.Combine(fullPath, ".llm-wiki", "wiki.ps1");
        if (!File.Exists(wikiPath)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.RepositoryNotFound,
                $"The configured repository root does not contain {wikiPath}.");
        }

        return fullPath;
    }
}
