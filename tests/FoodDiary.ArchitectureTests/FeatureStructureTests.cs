namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class FeatureStructureTests {
    [Fact]
    public void Application_Features_HaveCommandsOrQueriesFolders() {
        string root = GetRepositoryRoot();
        string applicationPath = Path.Combine(root, "FoodDiary.Application");
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "Common" };

        string[] featureDirectories = [.. Directory.GetDirectories(applicationPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Where(name => !excluded.Contains(name))];

        Assert.NotEmpty(featureDirectories);

        foreach (string? feature in featureDirectories) {
            string featurePath = Path.Combine(applicationPath, feature);
            bool hasCommands = Directory.Exists(Path.Combine(featurePath, "Commands"));
            bool hasQueries = Directory.Exists(Path.Combine(featurePath, "Queries"));
            bool hasCommon = Directory.Exists(Path.Combine(featurePath, "Common"));
            Assert.True(hasCommands || hasQueries || hasCommon,
                $"Feature '{feature}' should contain Commands, Queries, and/or Common folder.");
        }
    }

    [Fact]
    public void PresentationApi_FeatureFolders_ContainControllers() {
        string root = GetRepositoryRoot();
        string featuresPath = Path.Combine(root, "FoodDiary.Presentation.Api", "Features");
        string[] featureDirectories = Directory.GetDirectories(featuresPath);

        Assert.NotEmpty(featureDirectories);

        foreach (string featurePath in featureDirectories) {
            string[] controllers = Directory.GetFiles(featurePath, "*Controller.cs");
            Assert.NotEmpty(controllers);
        }
    }

    [Theory]
    [InlineData("FoodDiary.Application.Abstractions", "FoodDiary.Application.Abstractions")]
    [InlineData("FoodDiary.Application", "FoodDiary.Application")]
    [InlineData("FoodDiary.Domain", "FoodDiary.Domain")]
    [InlineData("FoodDiary.Infrastructure", "FoodDiary.Infrastructure")]
    [InlineData("FoodDiary.Integrations", "FoodDiary.Integrations")]
    [InlineData("FoodDiary.JobManager", "FoodDiary.JobManager")]
    [InlineData("MailInbox/FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Application")]
    [InlineData("MailInbox/FoodDiary.MailInbox.Client", "FoodDiary.MailInbox.Client")]
    [InlineData("MailInbox/FoodDiary.MailInbox.Domain", "FoodDiary.MailInbox.Domain")]
    [InlineData("MailInbox/FoodDiary.MailInbox.Infrastructure", "FoodDiary.MailInbox.Infrastructure")]
    [InlineData("MailInbox/FoodDiary.MailInbox.Presentation", "FoodDiary.MailInbox.Presentation")]
    [InlineData("MailInbox/FoodDiary.MailInbox.WebApi", "FoodDiary.MailInbox.WebApi")]
    [InlineData("MailRelay/FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Application")]
    [InlineData("MailRelay/FoodDiary.MailRelay.Client", "FoodDiary.MailRelay.Client")]
    [InlineData("MailRelay/FoodDiary.MailRelay.Domain", "FoodDiary.MailRelay.Domain")]
    [InlineData("MailRelay/FoodDiary.MailRelay.Infrastructure", "FoodDiary.MailRelay.Infrastructure")]
    [InlineData("MailRelay/FoodDiary.MailRelay.Presentation", "FoodDiary.MailRelay.Presentation")]
    [InlineData("MailRelay/FoodDiary.MailRelay.WebApi", "FoodDiary.MailRelay.WebApi")]
    [InlineData("FoodDiary.Presentation.Api", "FoodDiary.Presentation.Api")]
    [InlineData("FoodDiary.Telegram.Bot", "FoodDiary.Telegram.Bot")]
    [InlineData("FoodDiary.Web.Api", "FoodDiary.Web.Api")]
    public void Namespaces_Match_ProjectFolderStructure(string projectFolder, string namespaceRoot) {
        string root = GetRepositoryRoot();
        string projectPath = Path.Combine(root, projectFolder);
        string[] sourceFiles = [.. SourceScanner.SourceFiles(projectPath)];

        Assert.NotEmpty(sourceFiles);

        foreach (string? sourceFile in sourceFiles) {
            string? namespaceFromFile = CSharpSyntaxReader.ReadNamespace(sourceFile);
            if (string.IsNullOrWhiteSpace(namespaceFromFile)) {
                // Entry points may use top-level statements without explicit namespace.
                // AssemblyInfo files may contain only assembly-level attributes.
                string fileName = Path.GetFileName(sourceFile);
                if (string.Equals(fileName, "Program.cs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "GlobalUsings.cs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                Assert.False(string.IsNullOrWhiteSpace(namespaceFromFile),
                    $"Namespace declaration not found in '{sourceFile}'.");
            }

            string relativeDirectory =
                Path.GetDirectoryName(Path.GetRelativePath(projectPath, sourceFile)) ?? string.Empty;
            string namespaceSuffix = relativeDirectory
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');

            string expectedNamespace = string.IsNullOrWhiteSpace(namespaceSuffix)
                ? namespaceRoot
                : $"{namespaceRoot}.{namespaceSuffix}";

            Assert.Equal(expectedNamespace, namespaceFromFile);
        }
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

}
