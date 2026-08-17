using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ExternalHttpClientGuardrailTests {
    private static readonly HashSet<string> UnboundedReadMethods = new(StringComparer.Ordinal) {
        "ReadAsStringAsync",
        "ReadAsByteArrayAsync",
        "ReadFromJsonAsync",
        "GetFromJsonAsync",
    };

    [Fact]
    public void Integrations_DoNotBufferExternalResponseBodiesOutsideBoundedReader() {
        string[] violations = [.. ReadInvocations()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax access &&
                UnboundedReadMethods.Contains(access.Name.Identifier.ValueText) &&
                !string.Equals(access.Expression.ToString(), "BoundedHttpContentReader", StringComparison.Ordinal))
            .Select(FormatViolation)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void Integrations_DirectHttpClientRequests_CompleteAtResponseHeaders() {
        string[] violations = [.. ReadInvocations()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "httpClient" },
                Name.Identifier.ValueText: "SendAsync",
            })
            .Where(invocation => !invocation.ArgumentList.Arguments.Any(argument =>
                string.Equals(
                    argument.Expression.ToString(),
                    "HttpCompletionOption.ResponseHeadersRead",
                    StringComparison.Ordinal)))
            .Select(FormatViolation)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static IEnumerable<InvocationExpressionSyntax> ReadInvocations() {
        string integrationsRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Integrations");
        return SourceScanner.SourceFiles(integrationsRoot)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
                .GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>());
    }

    private static string FormatViolation(InvocationExpressionSyntax invocation) {
        FileLinePositionSpan span = invocation.SyntaxTree.GetLineSpan(invocation.Span);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, span.Path)}:{span.StartLinePosition.Line + 1}");
    }
}
