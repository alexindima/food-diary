using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class InitializerGuardrailTests {
    [Fact]
    public void InitializerProject_ReferencesOnlyApplicationInfrastructureAndOperationalPackages() {
        const string relativeProjectPath = "FoodDiary.Initializer/FoodDiary.Initializer.csproj";
        string[] expectedProjectReferences = [
            "FoodDiary.Application",
            "FoodDiary.Application.Marketing",
            "FoodDiary.Infrastructure",
        ];
        string[] expectedPackageReferences = [
            "Microsoft.EntityFrameworkCore",
            "Microsoft.EntityFrameworkCore.Relational",
            "Microsoft.Extensions.Hosting",
        ];

        string[] projectReferences = ProjectReferenceReader.ReadProjectReferences(relativeProjectPath);
        string[] packageReferences = ProjectReferenceReader.ReadPackageReferences(relativeProjectPath);

        Assert.Equal(expectedProjectReferences, projectReferences);
        Assert.Equal(expectedPackageReferences, packageReferences);
    }

    [Fact]
    public void InitializerRootFolders_StayLimitedToConsoleHostStructure() {
        string initializerRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Initializer");
        string[] allowedDirectories = [
            "Properties",
        ];

        string[] unexpectedDirectories = [.. Directory.GetDirectories(initializerRoot)
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
    public void InitializerSource_DoesNotReferenceHttpPresentationHostOrSchedulerSurface() {
        string initializerRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Initializer");

        string[] violations = SourceScanner.FindLinePatternViolations(initializerRoot, [
            "using FoodDiary.Presentation.Api",
            "using FoodDiary.Web.Api",
            "Microsoft.AspNetCore.Mvc",
            "ControllerBase",
            "IActionResult",
            "HttpContext",
            "MapGet(",
            "MapPost(",
            "MapPut(",
            "MapPatch(",
            "MapDelete(",
            "Hangfire",
            "IHostedService",
            "BackgroundService",
        ]);

        Assert.Empty(violations);
    }

    [Fact]
    public void InitializerProductionCode_UsesCancellationTokenNoneOnlyAtConsoleEntrypointBoundary() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string initializerRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Initializer");
        string programPath = Path.Combine(initializerRoot, "Program.cs");

        string[] violations = [.. SourceScanner.SourceFiles(initializerRoot)
            .Where(path => !string.Equals(path, programPath, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Contains("CancellationToken.None", StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
}
