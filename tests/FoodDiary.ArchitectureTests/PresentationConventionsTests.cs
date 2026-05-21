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

        var violations = SourceScanner.FindLinePatternViolations(featuresPath, ["Mediator.Send("]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationApi_SourceFiles_DoNotUseContractsNamespaces() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");

        var violations = SourceScanner.FindLinePatternViolations(presentationRoot, ["using FoodDiary.Contracts"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationControllersAndHubs_DoNotParseClaimsDirectly() {
        var root = GetRepositoryRoot();
        var presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");
        var scopedDirectories = new[] {
            Path.Combine(presentationRoot, "Features"),
            Path.Combine(presentationRoot, "Hubs"),
            Path.Combine(presentationRoot, "Controllers")
        };

        var violations = SourceScanner.FindLinePatternViolations(
            scopedDirectories,
            ["FindFirst(", "FindFirstValue(", "ClaimTypes."]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationControllers_DoNotReturnAdHocHttpResults() {
        var root = GetRepositoryRoot();
        var featuresPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Features");
        var bannedPatterns = new[] {
            "BadRequest(",
            "Unauthorized(",
            "Conflict(",
            "NotFound(",
            "Forbid(",
            "StatusCode("
        };

        var violations = SourceScanner.FindLinePatternViolations(featuresPath, bannedPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void ResultExtensions_UseDedicatedPresentationErrorMapper() {
        var root = GetRepositoryRoot();
        var resultExtensionsPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Extensions", "ResultExtensions.cs");
        var source = File.ReadAllText(resultExtensionsPath);

        Assert.Contains("PresentationErrorHttpMapper.MapStatusCode", source, StringComparison.Ordinal);
        Assert.DoesNotContain("code switch", source, StringComparison.Ordinal);
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
