using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ProjectFileConventionTests {
    [Fact]
    public async Task ProjectFiles_MatchAutomaticFormatterAsync() {
        var startInfo = new System.Diagnostics.ProcessStartInfo("pwsh") {
            WorkingDirectory = ArchitectureTestPaths.RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(ArchitectureTestPaths.FromRoot("scripts", "Format-ProjectFiles.ps1"));
        startInfo.ArgumentList.Add("-Check");
        using System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60_000)) {
            process.Kill(entireProcessTree: true);
            Assert.Fail("Project formatter exceeded 60 seconds.");
        }
        Assert.True(process.ExitCode == 0,
            $"Run ./scripts/Format-ProjectFiles.ps1 to fix project formatting.{Environment.NewLine}" +
            await output + await error);
    }

    [Fact]
    public void ProjectGroups_UseMultilineLayoutAndBlankLineSeparators() {
        string[] violations = [.. Directory
            .GetFiles(ArchitectureTestPaths.RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .SelectMany(FindGroupLayoutViolations)
            .Order(StringComparer.Ordinal)];

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> FindGroupLayoutViolations(string projectPath) {
        var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        foreach (XElement group in document.Descendants().Where(static element =>
                     element.Name.LocalName is "ItemGroup" or "PropertyGroup")) {
            string location = $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, projectPath)}:{((IXmlLineInfo)group).LineNumber.ToString(CultureInfo.InvariantCulture)}";
            int depth = group.Ancestors().Count();
            string indent = new(' ', depth * 2);
            XNode[] nodes = [.. group.Nodes()];
            if (nodes.Length > 0 && (nodes[0] is not XText leading ||
                !leading.Value.Replace("\r\n", "\n", StringComparison.Ordinal).Equals("\n" + indent + "  ", StringComparison.Ordinal) ||
                nodes[^1] is not XText trailing ||
                !trailing.Value.Replace("\r\n", "\n", StringComparison.Ordinal).Equals("\n" + indent, StringComparison.Ordinal))) {
                yield return $"{location}: Group tags and children must use separate lines with two-space indentation.";
            }

            foreach (XElement child in group.Elements()) {
                if (child.PreviousNode is not XText spacing ||
                    !spacing.Value.Replace("\r\n", "\n", StringComparison.Ordinal).EndsWith("\n" + indent + "  ", StringComparison.Ordinal)) {
                    yield return $"{location}: Each group element must start on a separate indented line.";
                }
            }

            if (group.PreviousNode is XText separator && separator.PreviousNode is XElement previous &&
                previous.Name.LocalName is "ItemGroup" or "PropertyGroup" &&
                !separator.Value.Replace("\r\n", "\n", StringComparison.Ordinal).Equals("\n\n" + indent, StringComparison.Ordinal)) {
                yield return $"{location}: Adjacent groups must be separated by one blank line.";
            } else if (group.PreviousNode is XElement adjacent && adjacent.Name.LocalName is "ItemGroup" or "PropertyGroup") {
                yield return $"{location}: Adjacent groups must be separated by one blank line.";
            }
        }
    }

    [Fact]
    public void ItemGroups_DoNotMixReferenceTypes() {
        string[] violations = [.. Directory
            .GetFiles(ArchitectureTestPaths.RepositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .SelectMany(FindMixedReferenceGroups)
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            $"PackageReference, ProjectReference and FrameworkReference items must use separate ItemGroups.{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> FindMixedReferenceGroups(string projectPath) {
        var document = XDocument.Load(projectPath, LoadOptions.SetLineInfo);

        return document
            .Descendants()
            .Where(static element => element.Name.LocalName.Equals("ItemGroup", StringComparison.Ordinal))
            .Where(static group => group.Elements()
                .Select(static element => element.Name.LocalName)
                .Where(static name => name is "PackageReference" or "ProjectReference" or "FrameworkReference")
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
            .Select(group => {
                string relativePath = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, projectPath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                int line = ((IXmlLineInfo)group).LineNumber;
                return $"{relativePath}:{line.ToString(CultureInfo.InvariantCulture)}: Mixed reference types in ItemGroup.";
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
