using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ResourcesGuardrailTests {
    [Fact]
    public void ResourcesProject_ReferencesOnlyApplicationAbstractionsAndNoPackages() {
        const string relativeProjectPath = "FoodDiary.Resources/FoodDiary.Resources.csproj";
        string[] expectedProjectReferences = [
            "FoodDiary.Application.Abstractions",
        ];

        string[] projectReferences = ProjectReferenceReader.ReadProjectReferences(relativeProjectPath);
        string[] packageReferences = ProjectReferenceReader.ReadPackageReferences(relativeProjectPath);

        Assert.Equal(expectedProjectReferences, projectReferences);
        Assert.Empty(packageReferences);
    }

    [Fact]
    public void ResourcesRootFolders_StayLimitedToResourceProviderStructure() {
        string resourcesRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Resources");
        string[] allowedDirectories = [
            "Notifications",
            "Reports",
        ];

        string[] unexpectedDirectories = [.. Directory.GetDirectories(resourcesRoot)
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
    public void RussianResourceFiles_HaveMatchingNeutralResourceFiles() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string resourcesRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Resources");

        string[] violations = [.. Directory.GetFiles(resourcesRoot, "*.ru.resx", SearchOption.AllDirectories)
            .Where(path => !File.Exists(path.Replace(".ru.resx", ".resx", StringComparison.OrdinalIgnoreCase)))
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void RussianResourceFiles_DoNotContainReplacementCharacters() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string resourcesRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Resources");

        string[] violations = [.. Directory.GetFiles(resourcesRoot, "*.ru.resx", SearchOption.AllDirectories)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line }))
            .Where(static entry => entry.line.Any(static character => character == '\uFFFD'))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }
}
