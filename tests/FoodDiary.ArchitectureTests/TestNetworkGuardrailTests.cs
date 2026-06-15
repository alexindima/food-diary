using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class TestNetworkGuardrailTests {
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    [Fact]
    public void Tests_DoNotConnectToExternalHostsDirectly() {
        string[] violations = [.. EnumerateTestSourceFiles()
            .SelectMany(FindExternalConnectAsyncCalls)
            .OrderBy(static violation => violation.Path, PathComparer)
            .ThenBy(static violation => violation.Line)
            .Select(static violation => violation.Format())];

        Assert.True(
            violations.Length == 0,
            "Tests should not connect to real external hosts. Use loopback listeners, Testcontainers, WebApplicationFactory, or recording HTTP handlers instead. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> EnumerateTestSourceFiles() =>
        Directory.EnumerateFiles(ArchitectureTestPaths.RepositoryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !ArchitectureTestPaths.IsGeneratedOrBuildPath(path))
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<ExternalConnectViolation> FindExternalConnectAsyncCalls(string path) {
        Microsoft.CodeAnalysis.SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
        CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !string.Equals(memberAccess.Name.Identifier.ValueText, "ConnectAsync", StringComparison.Ordinal) ||
                invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not LiteralExpressionSyntax literal ||
                literal.Token.Value is not string host ||
                IsLocalHost(host)) {
                continue;
            }

            yield return new ExternalConnectViolation(
                Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path),
                invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                host);
        }
    }

    private static bool IsLocalHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
        host.StartsWith("127.", StringComparison.Ordinal);

    [ExcludeFromCodeCoverage]
    private sealed record ExternalConnectViolation(string Path, int Line, string Host) {
        public string Format() => string.Create(CultureInfo.InvariantCulture, $"{Path}:{Line} ConnectAsync(\"{Host}\")");
    }
}
