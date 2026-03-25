using System.Text.RegularExpressions;

namespace FoodDiary.ArchitectureTests;

public class PresentationConventionsTests {
    [Fact]
    public void PresentationApi_ProjectGuide_Exists() {
        var root = GetRepositoryRoot();
        var guidePath = Path.Combine(root, "FoodDiary.Presentation.Api", "AGENTS.md");

        Assert.True(File.Exists(guidePath), $"Expected project guide at '{guidePath}'.");
    }

    [Fact]
    public void PresentationControllers_DoNotCallMediatorSendDirectly() {
        var root = GetRepositoryRoot();
        var featuresPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Features");

        var violations = Directory.GetFiles(featuresPath, "*Controller.cs", SearchOption.AllDirectories)
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.Contains("Mediator.Send(", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationApi_SourceFiles_DoNotUseContractsNamespaces() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");

        var violations = Directory.GetFiles(presentationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => Regex.IsMatch(entry.line, @"^\s*using\s+FoodDiary\.Contracts", RegexOptions.CultureInvariant))
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
}
