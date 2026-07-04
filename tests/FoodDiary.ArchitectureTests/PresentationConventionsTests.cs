using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class PresentationConventionsTests {
    [Fact]
    public void PresentationApi_ProjectGuide_Exists() {
        string root = GetRepositoryRoot();
        string guidePath = Path.Combine(root, "FoodDiary.Presentation.Api", "AGENTS.md");

        Assert.True(File.Exists(guidePath), $"Expected project guide at '{guidePath}'.");
    }

    [Fact]
    public void PresentationControllers_DoNotCallMediatorSendDirectly() {
        string root = GetRepositoryRoot();
        string featuresPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Features");

        string[] violations = SourceScanner.FindLinePatternViolations(featuresPath, ["Mediator.Send("]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationApi_SourceFiles_DoNotUseContractsNamespaces() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoot, ["using FoodDiary.Contracts"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationControllersAndHubs_DoNotParseClaimsDirectly() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");
        string[] scopedDirectories = [
            Path.Combine(presentationRoot, "Features"),
            Path.Combine(presentationRoot, "Hubs"),
            Path.Combine(presentationRoot, "Controllers"),
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(
            scopedDirectories,
            ["FindFirst(", "FindFirstValue(", "ClaimTypes."]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationControllers_DoNotReturnAdHocHttpResults() {
        string root = GetRepositoryRoot();
        string featuresPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Features");
        string[] bannedPatterns = [
            "BadRequest(",
            "Unauthorized(",
            "Conflict(",
            "NotFound(",
            "Forbid(",
            "StatusCode(",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(featuresPath, bannedPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationFeatureControllers_UseBaseControllerExceptDocumentedPresentationOnlyEndpoints() {
        string root = GetRepositoryRoot();
        string featuresPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Features");
        string[] allowedFiles = [
            Path.Combine(featuresPath, "Admin", "AdminTelemetryController.cs"),
            Path.Combine(featuresPath, "Logs", "LogsController.cs"),
        ];

        string[] violations = [.. SourceScanner.SourceFiles(featuresPath)
            .Where(path => !allowedFiles.Contains(path, StringComparer.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry => entry.line.Contains(": ControllerBase", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationFeatureControllers_AreSealed() {
        string root = GetRepositoryRoot();
        string featuresPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Features");

        string[] violations = [.. SourceScanner.SourceFiles(featuresPath)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(entry =>
                entry.line.Contains(" class ", StringComparison.Ordinal) &&
                entry.line.Contains("Controller", StringComparison.Ordinal) &&
                !entry.line.Contains(" sealed ", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void ResultExtensions_UseDedicatedPresentationErrorMapper() {
        string root = GetRepositoryRoot();
        string resultExtensionsPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Extensions", "ResultExtensions.cs");
        string source = File.ReadAllText(resultExtensionsPath);

        Assert.Contains("PresentationErrorHttpMapper.MapStatusCode", source, StringComparison.Ordinal);
        Assert.DoesNotContain("code switch", source, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot() {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null) {
            string solutionPath = Path.Combine(current.FullName, "FoodDiary.slnx");
            if (File.Exists(solutionPath)) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
