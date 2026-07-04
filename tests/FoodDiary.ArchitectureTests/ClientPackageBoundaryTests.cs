namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ClientPackageBoundaryTests {
    [Theory]
    [InlineData("FoodDiary.MailRelay.Client", "MailRelay/FoodDiary.MailRelay.Client")]
    [InlineData("FoodDiary.MailInbox.Client", "MailInbox/FoodDiary.MailInbox.Client")]
    public void ClientPackages_DoNotReferenceServerSideNamespaces(string projectName, string projectFolder) {
        string clientRoot = ArchitectureTestPaths.FromRoot(projectFolder);
        string boundedContextPrefix = projectName[..projectName.LastIndexOf('.')];
        string[] forbiddenPatterns = [
            $"{boundedContextPrefix}.Application",
            $"{boundedContextPrefix}.Domain",
            $"{boundedContextPrefix}.Infrastructure",
            $"{boundedContextPrefix}.Presentation",
            $"{boundedContextPrefix}.WebApi",
            "Microsoft.AspNetCore",
            "ControllerBase",
            "HttpContext",
            "IEndpointRouteBuilder",
            "Npgsql",
            "RabbitMQ",
            "MailKit",
            "MimeKit",
            "SmtpServer",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(clientRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("MailRelay/FoodDiary.MailRelay.Client/FoodDiary.MailRelay.Client.csproj")]
    [InlineData("MailInbox/FoodDiary.MailInbox.Client/FoodDiary.MailInbox.Client.csproj")]
    public void ClientPackages_DoNotReferenceServerSidePackages(string relativeProjectPath) {
        var forbiddenPackages = new HashSet<string>(StringComparer.Ordinal) {
            "Microsoft.AspNetCore.App",
            "Microsoft.EntityFrameworkCore",
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "RabbitMQ.Client",
            "MailKit",
            "MimeKit",
            "SmtpServer",
        };

        string[] violations = [.. ProjectReferenceReader.ReadPackageReferences(relativeProjectPath)
            .Where(forbiddenPackages.Contains)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("FoodDiary.MailRelay.Client", "MailRelay/FoodDiary.MailRelay.Client")]
    [InlineData("FoodDiary.MailInbox.Client", "MailInbox/FoodDiary.MailInbox.Client")]
    public void ClientPackages_ExposeOnlyClientModelsOptionsAndRegistrationSurface(string projectName, string projectFolder) {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string clientRoot = ArchitectureTestPaths.FromRoot(projectFolder);
        string contextName = GetBoundedContextName(projectName);
        var allowedRootFiles = new HashSet<string>(StringComparer.Ordinal) {
            $"I{contextName}Client.cs",
            $"{contextName}Client.cs",
        };

        string[] violations = [.. SourceScanner.SourceFiles(clientRoot)
            .Where(path => {
                string relative = Path.GetRelativePath(clientRoot, path);
                return !relative.StartsWith($"Models{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                       !relative.StartsWith($"Options{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                       !relative.StartsWith($"Extensions{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                       !allowedRootFiles.Contains(relative);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static string GetBoundedContextName(string clientProjectName) {
        string[] segments = clientProjectName.Split('.');
        return segments[^2];
    }
}
