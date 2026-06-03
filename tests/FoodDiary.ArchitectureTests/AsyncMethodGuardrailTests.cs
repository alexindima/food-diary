namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class AsyncMethodGuardrailTests {
    private static readonly string[] BackendProjectFolders = [
        "FoodDiary.Application",
        "FoodDiary.Application.Abstractions",
        "FoodDiary.Domain",
        "FoodDiary.Infrastructure",
        "FoodDiary.Integrations",
        "FoodDiary.JobManager",
        "FoodDiary.MailInbox.Application",
        "FoodDiary.MailInbox.Client",
        "FoodDiary.MailInbox.Domain",
        "FoodDiary.MailInbox.Infrastructure",
        "FoodDiary.MailInbox.Presentation",
        "FoodDiary.MailInbox.WebApi",
        "FoodDiary.MailRelay.Application",
        "FoodDiary.MailRelay.Client",
        "FoodDiary.MailRelay.Domain",
        "FoodDiary.MailRelay.Infrastructure",
        "FoodDiary.MailRelay.Initializer",
        "FoodDiary.MailRelay.Presentation",
        "FoodDiary.MailRelay.WebApi",
        "FoodDiary.Presentation.Api",
        "FoodDiary.Telegram.Bot",
        "FoodDiary.Web.Api",
    ];

    private static readonly HashSet<string> AllowedNonAsyncSuffixNames = new(StringComparer.Ordinal) {
        "Execute",
        "Handle",
        "HandleAccepted",
        "HandleCreated",
        "HandleFile",
        "HandleNoContent",
        "HandleObservedCreated",
        "HandleObservedNoContent",
        "HandleObservedOk",
        "HandleOk",
    };

    [Fact]
    public void BackendAsyncMethods_UseAsyncSuffixUnlessFrameworkEntryPoint() {
        var root = GetRepositoryRoot();
        var violations = GetBackendMethods(root)
            .Where(static method => method.IsAsyncLike)
            .Where(method => method.Name.EndsWith("Async", StringComparison.Ordinal) is false)
            .Where(method => AllowedNonAsyncSuffixNames.Contains(method.Name) is false)
            .Where(method => IsControllerAction(method) is false)
            .Select(method => method.Format(root))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Async methods should use the Async suffix unless they implement a framework entrypoint. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void BackendSynchronousMethods_DoNotUseAsyncSuffix() {
        var root = GetRepositoryRoot();
        var violations = GetBackendMethods(root)
            .Where(static method => method.IsAsyncLike is false)
            .Where(method => method.Name.EndsWith("Async", StringComparison.Ordinal))
            .Where(method => IsFrameworkAsyncHook(method) is false)
            .Select(method => method.Format(root))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Synchronous methods should not use the Async suffix. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void BackendAsyncMethods_AcceptCancellationTokenUnlessFrameworkEntryPoint() {
        var root = GetRepositoryRoot();
        var violations = GetBackendMethods(root)
            .Where(static method => method.IsAsyncLike)
            .Where(static method => method.Parameters.Contains("CancellationToken", StringComparison.Ordinal) is false)
            .Where(method => IsCancellationTokenProvidedByFramework(method) is false)
            .Select(method => method.Format(root))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Async methods should accept a CancellationToken unless cancellation is provided by the framework context. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static IReadOnlyList<MethodDeclaration> GetBackendMethods(string repositoryRoot) =>
        BackendProjectFolders
            .Select(folder => Path.Combine(repositoryRoot, folder))
            .Where(Directory.Exists)
            .SelectMany(SourceScanner.SourceFiles)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .SelectMany(CSharpSyntaxReader.ReadMethods)
            .Select(static method => new MethodDeclaration(
                method.Path,
                method.Line,
                method.ReturnType,
                method.Name,
                method.Parameters,
                method.IsAsyncLike,
                method.IsControllerAction))
            .ToArray();

    private static bool IsControllerAction(MethodDeclaration method) =>
        method.IsControllerAction;

    private static bool IsCancellationTokenProvidedByFramework(MethodDeclaration method) =>
        IsControllerAction(method) ||
        method.Path.Contains($"{Path.DirectorySeparatorChar}Filters{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        method.Path.Contains($"{Path.DirectorySeparatorChar}Middleware{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        IsFrameworkAsyncHook(method) ||
        string.Equals(method.Name, "Execute", StringComparison.Ordinal);

    private static bool IsFrameworkAsyncHook(MethodDeclaration method) =>
        string.Equals(method.Name, "OnActionExecutionAsync", StringComparison.Ordinal) ||
        string.Equals(method.Name, "OnAuthorizationAsync", StringComparison.Ordinal) ||
        string.Equals(method.Name, "BindModelAsync", StringComparison.Ordinal) ||
        string.Equals(method.Name, "InvokeAsync", StringComparison.Ordinal) ||
        string.Equals(method.Name, "TryHandleAsync", StringComparison.Ordinal) ||
        string.Equals(method.Name, "CheckHealthAsync", StringComparison.Ordinal);

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

    [ExcludeFromCodeCoverage]
    private sealed record MethodDeclaration(
        string Path,
        int Line,
        string ReturnType,
        string Name,
        string Parameters,
        bool IsAsyncLike,
        bool IsControllerAction) {

        public string Format(string repositoryRoot) =>
            $"{System.IO.Path.GetRelativePath(repositoryRoot, Path)}:{Line} {ReturnType} {Name}({Parameters})";
    }
}
