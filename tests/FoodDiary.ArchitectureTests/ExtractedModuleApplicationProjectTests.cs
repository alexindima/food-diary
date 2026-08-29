using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ExtractedModuleApplicationProjectTests {
    public static TheoryData<string> ExtractedModules => [
        "Fasting",
        "Hydration",
        "WeeklyGoals",
    ];

    [Theory]
    [MemberData(nameof(ExtractedModules))]
    public void ApplicationProject_IsARealProjectUnderApplication_WithDefaultSourceItems(string module) {
        string moduleRoot = ArchitectureTestPaths.FromRoot("Modules", module);
        string expectedProjectName = $"FoodDiary.Modules.{module}.Application.csproj";
        string applicationProject = Path.Combine(moduleRoot, "Application", expectedProjectName);
        string legacyRootProject = Path.Combine(moduleRoot, $"FoodDiary.Modules.{module}.csproj");

        Assert.True(File.Exists(applicationProject), $"Expected extracted Application project '{applicationProject}'.");
        Assert.False(File.Exists(legacyRootProject), $"Legacy root project must not return: '{legacyRootProject}'.");

        var project = XDocument.Load(applicationProject);
        string? defaultCompileItems = project
            .Descendants("EnableDefaultCompileItems")
            .Select(static element => element.Value)
            .SingleOrDefault();
        string[] compileRemoves = [.. project
            .Descendants("Compile")
            .Select(static element => element.Attribute("Remove")?.Value)
            .Where(static value => value is not null)
            .Cast<string>()];

        Assert.False(string.Equals("false", defaultCompileItems, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(["Abstractions/**/*.cs"], compileRemoves);

        string[] ordinaryApplicationSources = [.. Directory
            .EnumerateFiles(Path.Combine(moduleRoot, "Application"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(
                Path.Combine(moduleRoot, "Application", "Abstractions") + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))];

        Assert.NotEmpty(ordinaryApplicationSources);
    }
}
