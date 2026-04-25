using System.Text.RegularExpressions;

namespace FoodDiary.ArchitectureTests;

public class FeatureStructureTests {
    [Fact]
    public void Application_Features_HaveCommandsOrQueriesFolders() {
        var root = GetRepositoryRoot();
        var applicationPath = Path.Combine(root, "FoodDiary.Application");
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "Common" };

        var featureDirectories = Directory.GetDirectories(applicationPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Where(name => !excluded.Contains(name))
            .ToArray();

        Assert.NotEmpty(featureDirectories);

        foreach (var feature in featureDirectories) {
            var featurePath = Path.Combine(applicationPath, feature);
            var hasCommands = Directory.Exists(Path.Combine(featurePath, "Commands"));
            var hasQueries = Directory.Exists(Path.Combine(featurePath, "Queries"));
            var hasCommon = Directory.Exists(Path.Combine(featurePath, "Common"));
            Assert.True(hasCommands || hasQueries || hasCommon,
                $"Feature '{feature}' should contain Commands, Queries, and/or Common folder.");
        }
    }

    [Fact]
    public void PresentationApi_FeatureFolders_ContainControllers() {
        var root = GetRepositoryRoot();
        var featuresPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Features");
        var featureDirectories = Directory.GetDirectories(featuresPath);

        Assert.NotEmpty(featureDirectories);

        foreach (var featurePath in featureDirectories) {
            var controllers = Directory.GetFiles(featurePath, "*Controller.cs");
            Assert.NotEmpty(controllers);
        }
    }

    [Theory]
    [InlineData("FoodDiary.Application", "FoodDiary.Application")]
    [InlineData("FoodDiary.Domain", "FoodDiary.Domain")]
    [InlineData("FoodDiary.Infrastructure", "FoodDiary.Infrastructure")]
    [InlineData("FoodDiary.JobManager", "FoodDiary.JobManager")]
    [InlineData("FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Application")]
    [InlineData("FoodDiary.MailInbox.Domain", "FoodDiary.MailInbox.Domain")]
    [InlineData("FoodDiary.MailInbox.Infrastructure", "FoodDiary.MailInbox.Infrastructure")]
    [InlineData("FoodDiary.MailInbox.Presentation", "FoodDiary.MailInbox.Presentation")]
    [InlineData("FoodDiary.MailInbox.WebApi", "FoodDiary.MailInbox.WebApi")]
    [InlineData("FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Application")]
    [InlineData("FoodDiary.MailRelay.Client", "FoodDiary.MailRelay.Client")]
    [InlineData("FoodDiary.MailRelay.Domain", "FoodDiary.MailRelay.Domain")]
    [InlineData("FoodDiary.MailRelay.Infrastructure", "FoodDiary.MailRelay.Infrastructure")]
    [InlineData("FoodDiary.MailRelay.Presentation", "FoodDiary.MailRelay.Presentation")]
    [InlineData("FoodDiary.MailRelay.WebApi", "FoodDiary.MailRelay.WebApi")]
    [InlineData("FoodDiary.Presentation.Api", "FoodDiary.Presentation.Api")]
    [InlineData("FoodDiary.Telegram.Bot", "FoodDiary.Telegram.Bot")]
    [InlineData("FoodDiary.Web.Api", "FoodDiary.Web.Api")]
    public void Namespaces_Match_ProjectFolderStructure(string projectFolder, string namespaceRoot) {
        var root = GetRepositoryRoot();
        var projectPath = Path.Combine(root, projectFolder);
        var sourceFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(sourceFiles);

        foreach (var sourceFile in sourceFiles) {
            var namespaceFromFile = ReadNamespace(sourceFile);
            if (string.IsNullOrWhiteSpace(namespaceFromFile)) {
                // Entry points may use top-level statements without explicit namespace.
                // AssemblyInfo files may contain only assembly-level attributes.
                var fileName = Path.GetFileName(sourceFile);
                if (string.Equals(fileName, "Program.cs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "GlobalUsings.cs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                Assert.False(string.IsNullOrWhiteSpace(namespaceFromFile),
                    $"Namespace declaration not found in '{sourceFile}'.");
            }

            var relativeDirectory =
                Path.GetDirectoryName(Path.GetRelativePath(projectPath, sourceFile)) ?? string.Empty;
            var namespaceSuffix = relativeDirectory
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');

            var expectedNamespace = string.IsNullOrWhiteSpace(namespaceSuffix)
                ? namespaceRoot
                : $"{namespaceRoot}.{namespaceSuffix}";

            Assert.Equal(expectedNamespace, namespaceFromFile);
        }
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

    private static string? ReadNamespace(string sourceFilePath) {
        var source = File.ReadAllText(sourceFilePath);
        var match = Regex.Match(source, @"^\s*namespace\s+([A-Za-z0-9_.]+)\s*(?:;|\{)", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : null;
    }
}
