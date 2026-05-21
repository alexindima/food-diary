namespace FoodDiary.ArchitectureTests;

public sealed class ClientPackageBoundaryTests {
    [Theory]
    [InlineData("FoodDiary.MailRelay.Client")]
    [InlineData("FoodDiary.MailInbox.Client")]
    public void ClientPackages_DoNotReferenceServerSideNamespaces(string projectFolder) {
        var clientRoot = ArchitectureTestPaths.FromRoot(projectFolder);
        var boundedContextPrefix = projectFolder[..projectFolder.LastIndexOf(".", StringComparison.Ordinal)];
        var forbiddenPatterns = new[] {
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
        };

        var violations = SourceScanner.FindLinePatternViolations(clientRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("FoodDiary.MailRelay.Client")]
    [InlineData("FoodDiary.MailInbox.Client")]
    public void ClientPackages_ExposeOnlyClientModelsOptionsAndRegistrationSurface(string projectFolder) {
        var root = ArchitectureTestPaths.RepositoryRoot;
        var clientRoot = ArchitectureTestPaths.FromRoot(projectFolder);
        var contextName = GetBoundedContextName(projectFolder);
        var allowedRootFiles = new HashSet<string>(StringComparer.Ordinal) {
            $"I{contextName}Client.cs",
            $"{contextName}Client.cs",
        };

        var violations = SourceScanner.SourceFiles(clientRoot)
            .Where(path => {
                var relative = Path.GetRelativePath(clientRoot, path);
                return relative.StartsWith($"Models{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false &&
                       relative.StartsWith($"Options{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false &&
                       relative.StartsWith($"Extensions{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false &&
                       allowedRootFiles.Contains(relative) is false;
            })
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    private static string GetBoundedContextName(string clientProjectName) {
        var segments = clientProjectName.Split('.');
        return segments[^2];
    }
}
