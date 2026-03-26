namespace FoodDiary.ArchitectureTests;

public sealed class ApplicationGuardrailTests {
    [Fact]
    public void ApplicationSourceFiles_DoNotUseEnumParseDirectly() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("Enum.Parse(", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationHandlersAndServices_DoNotUseDateTimeUtcNow_Directly() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            Path.Combine(applicationRoot, "Common", "Services", "SystemDateTimeProvider.cs"),
        };

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(path => allowedFiles.Contains(path) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("DateTime.UtcNow", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationServiceInterfaces_AsyncMethodsAcceptCancellationToken() {
        var root = GetRepositoryRoot();
        var servicesRoot = Path.Combine(root, "FoodDiary.Application", "Common", "Interfaces", "Services");

        var violations = Directory.GetFiles(servicesRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationPersistenceInterfaces_AsyncMethodsAcceptCancellationToken() {
        var root = GetRepositoryRoot();
        var persistenceRoot = Path.Combine(root, "FoodDiary.Application", "Common", "Interfaces", "Persistence");

        var violations = Directory.GetFiles(persistenceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => GetAsyncMethodSignatures(path)
                .Where(static signature => signature.Contains("CancellationToken", StringComparison.Ordinal) is false)
                .Select(signature => $"{Path.GetRelativePath(root, path)}: {signature}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationSourceFiles_DoNotUseCancellationTokenNone() {
        var root = GetRepositoryRoot();
        var applicationRoot = Path.Combine(root, "FoodDiary.Application");

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("CancellationToken.None", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    private static string GetRepositoryRoot() {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null) {
            var solutionPath = Path.Combine(current.FullName, "FoodDiary.slnx");
            if (File.Exists(solutionPath)) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private static IEnumerable<string> GetAsyncMethodSignatures(string path) {
        var content = File.ReadAllText(path);
        var normalized = content.ReplaceLineEndings("\n");
        var matches = System.Text.RegularExpressions.Regex.Matches(
            normalized,
            @"Task(?:<[^;]+?>)?\s+\w+Async\s*\((.*?)\)\s*;",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        foreach (System.Text.RegularExpressions.Match match in matches) {
            yield return match.Value.ReplaceLineEndings(" ").Replace('\n', ' ').Trim();
        }
    }
}
