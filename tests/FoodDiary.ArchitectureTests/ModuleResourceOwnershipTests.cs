using System.Globalization;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleResourceOwnershipTests {
    private static readonly string[] ResourceRoots = [
        "Modules/Export/Infrastructure/Resources",
        "Modules/Notifications/Infrastructure/Resources",
    ];

    [Fact]
    public void LocalizedResourceProviders_LiveOnlyWithTheirModuleOwners() {
        string[] ownedPaths = [
            "Modules/Export/Infrastructure/Resources/DiaryPdfReportResourceTextProvider.cs",
            "Modules/Export/Infrastructure/Resources/DiaryPdfReport.resx",
            "Modules/Export/Infrastructure/Resources/DiaryPdfReport.ru.resx",
            "Modules/Notifications/Infrastructure/Resources/NotificationResourceRenderer.cs",
            "Modules/Notifications/Infrastructure/Resources/NotificationTemplates.resx",
            "Modules/Notifications/Infrastructure/Resources/NotificationTemplates.ru.resx",
        ];
        string[] retiredPaths = [
            "FoodDiary.Resources/FoodDiary.Resources.csproj",
            "FoodDiary.Resources/Reports/DiaryPdfReportResourceTextProvider.cs",
            "FoodDiary.Resources/Notifications/NotificationResourceRenderer.cs",
        ];

        Assert.All(ownedPaths, path => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(path)), path));
        Assert.All(retiredPaths, path => Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(path)), path));
    }

    [Theory]
    [InlineData("Modules/Export/Infrastructure/FoodDiary.Modules.Export.Infrastructure.csproj")]
    [InlineData("Modules/Notifications/Infrastructure/FoodDiary.Modules.Notifications.Infrastructure.csproj")]
    public void ResourceOwnerProjects_DeclareEnglishNeutralLanguage(string projectPath) {
        var document = XDocument.Load(ArchitectureTestPaths.FromRoot(projectPath));
        string? neutralLanguage = document.Descendants("NeutralLanguage").Select(static element => element.Value).SingleOrDefault();

        Assert.Equal("en", neutralLanguage);
    }

    [Fact]
    public void RussianResourceFiles_HaveMatchingNeutralFilesAndValidEncoding() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] russianFiles = [.. ResourceRoots
            .Select(static resourceRoot => ArchitectureTestPaths.FromRoot(resourceRoot))
            .SelectMany(sourceRoot => Directory.GetFiles(sourceRoot, "*.ru.resx", SearchOption.AllDirectories))
            .Order(StringComparer.Ordinal)];
        string[] missingNeutralFiles = [.. russianFiles
            .Where(path => !File.Exists(path.Replace(".ru.resx", ".resx", StringComparison.OrdinalIgnoreCase)))
            .Select(path => Path.GetRelativePath(root, path))];
        string[] encodingViolations = [.. russianFiles
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { path, line, index }))
            .Where(static entry => entry.line.Any(static character => character == '\uFFFD'))
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))];

        Assert.Equal(2, russianFiles.Length);
        Assert.Empty(missingNeutralFiles);
        Assert.Empty(encodingViolations);
    }
}
