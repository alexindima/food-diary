using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    public void PresentationApi_RootFoldersStayLimitedToDocumentedStructure() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");
        string[] allowedDirectories = [
            "Authorization",
            "Controllers",
            "Extensions",
            "Features",
            "Filters",
            "Hubs",
            "Options",
            "Policies",
            "Responses",
            "Security",
            "Services",
            "Telemetry",
        ];

        string[] unexpectedDirectories = [.. Directory.GetDirectories(presentationRoot)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .Where(name => !name.Equals("bin", StringComparison.OrdinalIgnoreCase))
            .Where(name => !name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            .Where(name => !allowedDirectories.Contains(name, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(unexpectedDirectories);
    }

    [Fact]
    public void PresentationApi_PackageReferencesStayLimitedToHttpPresentationConcerns() {
        string[] expectedPackages = [
            "Asp.Versioning.Mvc",
        ];

        string[] packages = ProjectReferenceReader.ReadPackageReferences("FoodDiary.Presentation.Api/FoodDiary.Presentation.Api.csproj");

        Assert.Equal(expectedPackages, packages);
    }

    [Fact]
    public void PresentationFeatureFolders_StayLimitedToTransportPurposeFolders() {
        string root = GetRepositoryRoot();
        string[] featurePaths = GetFeatureRoots(root);
        var allowedPurposeFolders = new HashSet<string>(StringComparer.Ordinal) {
            "Mappings",
            "Models",
            "Requests",
            "Responses",
        };

        string[] violations = [.. featurePaths.SelectMany(Directory.GetDirectories)
            .SelectMany(featurePath => Directory.GetDirectories(featurePath))
            .Where(path => !allowedPurposeFolders.Contains(Path.GetFileName(path)))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationHttpRequestsAndQueries_LiveUnderFeatureRequestsFolders() {
        string root = GetRepositoryRoot();
        string[] presentationRoots = GetPresentationRoots(root);

        string[] violations = [.. SourceScanner.SourceFiles(presentationRoots)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line = line.Trim() }))
            .Where(static entry =>
                entry.line.StartsWith("public sealed record ", StringComparison.Ordinal) &&
                (entry.line.Contains("HttpRequest", StringComparison.Ordinal) ||
                 entry.line.Contains("HttpQuery", StringComparison.Ordinal)))
            .Where(static entry => !entry.path.Contains($"{Path.DirectorySeparatorChar}Requests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationHttpResponses_LiveUnderResponseFolders() {
        string root = GetRepositoryRoot();
        string[] presentationRoots = GetPresentationRoots(root);

        string[] violations = [.. SourceScanner.SourceFiles(presentationRoots)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line = line.Trim() }))
            .Where(static entry =>
                entry.line.StartsWith("public sealed record ", StringComparison.Ordinal) &&
                entry.line.Contains("HttpResponse", StringComparison.Ordinal))
            .Where(static entry => !entry.path.Contains($"{Path.DirectorySeparatorChar}Responses{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationControllers_DoNotCallMediatorSendDirectly() {
        string root = GetRepositoryRoot();
        string[] featurePaths = GetFeatureRoots(root);

        string[] violations = SourceScanner.FindLinePatternViolations(featurePaths, ["Mediator.Send("]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationRequestTelemetry_HasOneOperationMetricWriter() {
        string root = GetRepositoryRoot();
        string presentationRoot = Path.Combine(root, "FoodDiary.Presentation.Api");
        string[] expectedWriterFiles = [
            Path.Combine("FoodDiary.Presentation.Api", "Filters", "TelemetryActionFilter.cs"),
        ];

        string[] operationCounterWriters = FindFilesContaining(
            root,
            presentationRoot,
            "PresentationApiTelemetry.OperationCounter.Add");
        string[] operationDurationWriters = FindFilesContaining(
            root,
            presentationRoot,
            "PresentationApiTelemetry.OperationDuration.Record");

        Assert.Multiple(
            () => Assert.Equal(expectedWriterFiles, operationCounterWriters),
            () => Assert.Equal(expectedWriterFiles, operationDurationWriters));
    }

    [Fact]
    public void PresentationControllers_InjectOnlyPresentationSafeDependencies() {
        string root = GetRepositoryRoot();
        string[] presentationRoots = GetPresentationRoots(root);

        string[] violations = [.. presentationRoots.SelectMany(path => Directory.GetFiles(path, "*Controller.cs", SearchOption.AllDirectories))
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot()
                .DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(static declaration => declaration.Identifier.ValueText.EndsWith("Controller", StringComparison.Ordinal))
                .SelectMany(declaration => declaration.ParameterList?.Parameters ?? [])
                .Select(parameter => new { path, parameter }))
            .Where(static entry => !IsPresentationSafeControllerDependency(entry.parameter.Type?.ToString() ?? string.Empty))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.parameter.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {entry.parameter.Type}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationApi_SourceFiles_DoNotUseContractsNamespaces() {
        string root = GetRepositoryRoot();
        string[] presentationRoots = GetPresentationRoots(root);

        string[] violations = SourceScanner.FindLinePatternViolations(presentationRoots, ["using FoodDiary.Contracts"]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationControllersAndHubs_DoNotParseClaimsDirectly() {
        string root = GetRepositoryRoot();
        string[] scopedDirectories = [.. GetPresentationRoots(root)
            .SelectMany(path => new[] { "Features", "Hubs", "Controllers" }
                .Select(folder => Path.Combine(path, folder)))
            .Where(Directory.Exists)];

        string[] violations = SourceScanner.FindLinePatternViolations(
            scopedDirectories,
            ["FindFirst(", "FindFirstValue(", "ClaimTypes."]);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationControllers_DoNotReturnAdHocHttpResults() {
        string root = GetRepositoryRoot();
        string[] featurePaths = GetFeatureRoots(root);
        string[] bannedPatterns = [
            "BadRequest(",
            "Unauthorized(",
            "Conflict(",
            "NotFound(",
            "Forbid(",
            "StatusCode(",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(featurePaths, bannedPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void PresentationFeatureControllers_UseBaseControllerExceptDocumentedPresentationOnlyEndpoints() {
        string root = GetRepositoryRoot();
        string[] featurePaths = GetFeatureRoots(root);
        string[] allowedFiles = [
            Path.Combine(root, "Modules", "Admin", "Presentation", "Features", "Admin", "AdminTelemetryController.cs"),
            Path.Combine(root, "Modules", "Fasting", "Presentation", "Features", "Logs", "LogsController.cs"),
        ];

        string[] violations = [.. SourceScanner.SourceFiles(featurePaths)
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

    private static bool IsPresentationSafeControllerDependency(string parameterType) {
        return parameterType is "ISender" or "TimeProvider" or "IApiVersionInfo" ||
               parameterType.EndsWith("HttpProcessor", StringComparison.Ordinal) ||
               parameterType.StartsWith("ILogger<", StringComparison.Ordinal);
    }

    private static string[] GetPresentationRoots(string root) => [
        Path.Combine(root, "FoodDiary.Presentation.Api"),
        .. Directory.GetDirectories(Path.Combine(root, "Modules"), "Presentation", SearchOption.AllDirectories)
            .Where(path => Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length == 1),
    ];

    private static string[] GetFeatureRoots(string root) => [.. GetPresentationRoots(root)
        .Select(path => Path.Combine(path, "Features"))
        .Where(Directory.Exists)];

    private static string[] FindFilesContaining(string root, string scopedPath, string pattern) =>
        [.. SourceScanner.SourceFiles(scopedPath)
            .Where(path => File.ReadAllText(path).Contains(pattern, StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];
}
