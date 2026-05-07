using System.Text.RegularExpressions;

namespace FoodDiary.ArchitectureTests;

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
            .SelectMany(projectRoot => Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories))
            .Where(static path => IsGeneratedOrBuildPath(path) is false)
            .SelectMany(ReadMethods)
            .ToArray();

    private static IEnumerable<MethodDeclaration> ReadMethods(string path) {
        var content = File.ReadAllText(path).ReplaceLineEndings("\n");
        var matches = Regex.Matches(
            content,
            @"(?m)^\s*(?:\[[^\]]+\]\s*)*(?:(?:public|private|protected|internal|static|virtual|override|sealed|abstract|partial|new|extern|async)\s+)*(?<returnType>[A-Za-z_][A-Za-z0-9_.]*(?:<[^;\n]+?>)?(?:\[\])?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\((?<parameters>[^;{]*)\)\s*(?:where\s+[^{;]+)?(?:\{|;|=>)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        foreach (Match match in matches) {
            var returnType = match.Groups["returnType"].Value;
            if (IsDefinitelyNotMethodReturnType(returnType)) {
                continue;
            }

            yield return new MethodDeclaration(
                path,
                GetLineNumber(content, match.Index),
                returnType,
                match.Groups["name"].Value,
                match.Groups["parameters"].Value.ReplaceLineEndings(" ").Trim());
        }
    }

    private static bool IsControllerAction(MethodDeclaration method) =>
        method.Path.EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase) ||
        method.Path.EndsWith("ControllerBase.cs", StringComparison.OrdinalIgnoreCase);

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

    private static bool IsDefinitelyNotMethodReturnType(string returnType) =>
        string.Equals(returnType, "await", StringComparison.Ordinal) ||
        string.Equals(returnType, "return", StringComparison.Ordinal) ||
        string.Equals(returnType, "using", StringComparison.Ordinal) ||
        string.Equals(returnType, "var", StringComparison.Ordinal) ||
        string.Equals(returnType, "if", StringComparison.Ordinal) ||
        string.Equals(returnType, "while", StringComparison.Ordinal) ||
        string.Equals(returnType, "foreach", StringComparison.Ordinal) ||
        string.Equals(returnType, "switch", StringComparison.Ordinal);

    private static bool IsGeneratedOrBuildPath(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);

    private static int GetLineNumber(string content, int index) =>
        content.AsSpan(0, index).Count('\n') + 1;

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

    private sealed record MethodDeclaration(
        string Path,
        int Line,
        string ReturnType,
        string Name,
        string Parameters) {
        public bool IsAsyncLike {
            get {
                var normalizedReturnType = ReturnType.Replace(" ", "", StringComparison.Ordinal);

                return normalizedReturnType.Equals("Task", StringComparison.Ordinal) ||
                    normalizedReturnType.Equals("ValueTask", StringComparison.Ordinal) ||
                    normalizedReturnType.EndsWith(".Task", StringComparison.Ordinal) ||
                    normalizedReturnType.EndsWith(".ValueTask", StringComparison.Ordinal) ||
                    normalizedReturnType.StartsWith("Task<", StringComparison.Ordinal) ||
                    normalizedReturnType.StartsWith("ValueTask<", StringComparison.Ordinal) ||
                    normalizedReturnType.Contains(".Task<", StringComparison.Ordinal) ||
                    normalizedReturnType.Contains(".ValueTask<", StringComparison.Ordinal);
            }
        }

        public string Format(string repositoryRoot) =>
            $"{System.IO.Path.GetRelativePath(repositoryRoot, Path)}:{Line} {ReturnType} {Name}({Parameters})";
    }
}
