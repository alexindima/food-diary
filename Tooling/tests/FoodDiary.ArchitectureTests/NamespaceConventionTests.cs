using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class NamespaceConventionTests {
    [Fact]
    public void AllProjects_UseCanonicalNamespacesWithoutRootOverrides() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] projects = [.. RepositoryFileDiscovery.EnumerateFiles(root, "*.csproj")];
        Assert.NotEmpty(projects);
        Dictionary<string, string> roots = projects.ToDictionary(
            path => Path.GetDirectoryName(path)!,
            path => Path.GetFileNameWithoutExtension(path)!,
            StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();
        foreach (string project in projects) {
            Assert.Empty(XDocument.Load(project).Descendants("RootNamespace"));
        }

        int inspected = 0;
        foreach (string source in SourceScanner.SourceFiles(root)) {
            var directory = new DirectoryInfo(Path.GetDirectoryName(source)!);
            while (directory is not null && !roots.ContainsKey(directory.FullName)) {
                directory = directory.Parent;
            }
            if (directory is null) {
                continue;
            }
            string? actual = CSharpSyntaxReader.ReadNamespace(source);
            if (actual is null && (CSharpSyntaxReader.ReadTypeDeclarations(source).Count == 0 ||
                                  Path.GetFileName(source).StartsWith("Program.", StringComparison.Ordinal))) {
                continue;
            }
            inspected++;
            string suffix = (Path.GetDirectoryName(Path.GetRelativePath(directory.FullName, source)) ?? string.Empty)
                .Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
            string expected = roots[directory.FullName] + (suffix.Length == 0 ? string.Empty : "." + suffix);
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) {
                violations.Add($"{Path.GetRelativePath(root, source)}: expected {expected}, found {actual ?? "no namespace"}");
            }
        }
        Assert.True(inspected > 0);
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void NamespaceAnalyzer_IsGloballyEnabledWithoutProjectSuppressions() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.IDE0130.severity = error", editorConfig, StringComparison.Ordinal);
        Assert.Contains("dotnet_style_namespace_match_folder = true", editorConfig, StringComparison.Ordinal);
        foreach (string config in RepositoryFileDiscovery.EnumerateFiles(root, ".editorconfig")) {
            foreach (string line in File.ReadLines(config).Select(line => line.Trim())) {
                if (line.StartsWith("dotnet_diagnostic.IDE0130.severity", StringComparison.Ordinal)) {
                    Assert.Equal("dotnet_diagnostic.IDE0130.severity = error", line);
                }
                if (line.StartsWith("dotnet_style_namespace_match_folder", StringComparison.Ordinal)) {
                    Assert.Equal("dotnet_style_namespace_match_folder = true", line);
                }
            }
        }
        foreach (string project in RepositoryFileDiscovery.EnumerateFiles(root, "*.csproj")) {
            foreach (XElement suppression in XDocument.Load(project).Descendants()
                         .Where(element => element.Name.LocalName is "NoWarn" or "WarningsNotAsErrors")) {
                Assert.DoesNotContain("IDE0130", suppression.Value, StringComparison.OrdinalIgnoreCase);
            }
        }
        var props = XDocument.Load(Path.Combine(root, "Directory.Build.props"));
        foreach (string property in new[] { "RootNamespace", "ProjectDir" }) {
            XElement visible = Assert.Single(props.Descendants("CompilerVisibleProperty"),
                element => string.Equals((string?)element.Attribute("Include"), property, StringComparison.Ordinal));
            Assert.Null(visible.Attribute("Condition"));
            Assert.Null(visible.Parent?.Attribute("Condition"));
        }
    }
}
