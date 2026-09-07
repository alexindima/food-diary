namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class FeatureStructureTests {
    [Fact]
    public void Presentation_FeatureFolders_ContainControllers() {
        string root = GetRepositoryRoot();
        string[] featureDirectories = [
            .. Directory.GetDirectories(Path.Combine(root, "FoodDiary.Presentation.Api", "Features")),
            .. Directory.GetDirectories(Path.Combine(root, "Modules"), "Features", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "Presentation", StringComparison.Ordinal))
                .SelectMany(Directory.GetDirectories),
        ];

        Assert.NotEmpty(featureDirectories);

        foreach (string featurePath in featureDirectories) {
            string[] controllers = Directory.GetFiles(featurePath, "*Controller.cs");
            Assert.NotEmpty(controllers);
        }
    }

    [Theory]
    [InlineData("Shared/FoodDiary.Application.Contracts", "FoodDiary.Application.Abstractions")]
    [InlineData("FoodDiary.Application.Runtime", "FoodDiary.Application.Runtime")]
    [InlineData("FoodDiary.Infrastructure", "FoodDiary.Infrastructure")]
    [InlineData("FoodDiary.JobManager", "FoodDiary.JobManager")]
    [InlineData("Services/MailInbox/FoodDiary.MailInbox.Application", "FoodDiary.MailInbox.Application")]
    [InlineData("Services/MailInbox/FoodDiary.MailInbox.Client", "FoodDiary.MailInbox.Client")]
    [InlineData("Services/MailInbox/FoodDiary.MailInbox.Domain", "FoodDiary.MailInbox.Domain")]
    [InlineData("Services/MailInbox/FoodDiary.MailInbox.Infrastructure", "FoodDiary.MailInbox.Infrastructure")]
    [InlineData("Services/MailInbox/FoodDiary.MailInbox.Presentation", "FoodDiary.MailInbox.Presentation")]
    [InlineData("Services/MailInbox/FoodDiary.MailInbox.WebApi", "FoodDiary.MailInbox.WebApi")]
    [InlineData("Services/MailRelay/FoodDiary.MailRelay.Application", "FoodDiary.MailRelay.Application")]
    [InlineData("Services/MailRelay/FoodDiary.MailRelay.Client", "FoodDiary.MailRelay.Client")]
    [InlineData("Services/MailRelay/FoodDiary.MailRelay.Domain", "FoodDiary.MailRelay.Domain")]
    [InlineData("Services/MailRelay/FoodDiary.MailRelay.Infrastructure", "FoodDiary.MailRelay.Infrastructure")]
    [InlineData("Services/MailRelay/FoodDiary.MailRelay.Presentation", "FoodDiary.MailRelay.Presentation")]
    [InlineData("Services/MailRelay/FoodDiary.MailRelay.WebApi", "FoodDiary.MailRelay.WebApi")]
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
