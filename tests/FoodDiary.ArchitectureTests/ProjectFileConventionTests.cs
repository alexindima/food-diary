using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ProjectFileConventionTests {
    [Fact]
    public void ItemGroups_DoNotMixPackageAndProjectReferences() {
        string[] violations = [.. Directory
            .GetFiles(ArchitectureTestPaths.RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .SelectMany(FindMixedReferenceGroups)
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            $"PackageReference and ProjectReference items must use separate ItemGroups.{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> FindMixedReferenceGroups(string projectPath) {
        var document = XDocument.Load(projectPath, LoadOptions.SetLineInfo);

        return document
            .Descendants()
            .Where(static element => element.Name.LocalName.Equals("ItemGroup", StringComparison.Ordinal))
            .Where(static group => group.Elements().Any(element => element.Name.LocalName.Equals("PackageReference", StringComparison.Ordinal)) &&
                                   group.Elements().Any(element => element.Name.LocalName.Equals("ProjectReference", StringComparison.Ordinal)))
            .Select(group => {
                string relativePath = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, projectPath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                int line = ((IXmlLineInfo)group).LineNumber;
                return $"{relativePath}:{line.ToString(CultureInfo.InvariantCulture)}: Mixed PackageReference and ProjectReference items.";
            });
    }

    [Fact]
    public void UnconditionalProjectReferences_AreGroupedInSingleItemGroup() {
        string[] violations = [.. Directory
            .GetFiles(ArchitectureTestPaths.RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Where(static path => !path.Contains(
                $"{Path.DirectorySeparatorChar}.llm-wiki{Path.DirectorySeparatorChar}tools{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(FindSplitProjectReferenceGroups)
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            $"Unconditional ProjectReference items must be grouped in a single ItemGroup.{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ItemGroups_AreNotEmpty() {
        string[] violations = [.. Directory
            .GetFiles(ArchitectureTestPaths.RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Where(static path => !path.Contains(
                $"{Path.DirectorySeparatorChar}.llm-wiki{Path.DirectorySeparatorChar}tools{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(FindEmptyItemGroups)
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            $"ItemGroup elements without child elements have no effect and must be removed.{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> FindSplitProjectReferenceGroups(string projectPath) {
        var document = XDocument.Load(projectPath, LoadOptions.SetLineInfo);
        XElement[] groups = [.. document
            .Descendants("ItemGroup")
            .Where(static group => string.IsNullOrWhiteSpace((string?)group.Attribute("Condition")))
            .Where(static group => group.Elements("ProjectReference").Any())];

        if (groups.Length <= 1) {
            return [];
        }

        string relativePath = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, projectPath)
            .Replace(Path.DirectorySeparatorChar, '/');
        string lines = string.Join(
            ", ",
            groups.Select(static group => ((IXmlLineInfo)group).LineNumber));

        return [$"{relativePath}: ProjectReference ItemGroups start on lines {lines}."];
    }

    private static IEnumerable<string> FindEmptyItemGroups(string projectPath) {
        var document = XDocument.Load(projectPath, LoadOptions.SetLineInfo);

        return document
            .Descendants("ItemGroup")
            .Where(static group => !group.Elements().Any())
            .Select(group => {
                string relativePath = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, projectPath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                int line = ((IXmlLineInfo)group).LineNumber;
                return $"{relativePath}:{line.ToString(CultureInfo.InvariantCulture)}: Empty ItemGroup.";
            });
    }
}
