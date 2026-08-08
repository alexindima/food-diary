using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExpressionTreeSpanOverloadAnalyzerTests {
    [Fact]
    public async Task ArrayContainsInsideExpressionTreeReportsDiagnosticAsync() {
        const string source = """
            using System;
            using System.Linq.Expressions;

            public static class Example {
                public static Expression<Func<int, bool>> Create(int[] ids) =>
                    value => ids.Contains(value);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(source));

        Assert.Multiple(
            () => Assert.Equal(ExpressionTreeSpanOverloadAnalyzer.DiagnosticId, diagnostic.Id),
            () => Assert.Contains("non-span overload", diagnostic.GetMessage(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExplicitEnumerableContainsDoesNotReportDiagnosticAsync() {
        const string source = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;

            public static class Example {
                public static Expression<Func<int, bool>> Create(int[] ids) =>
                    value => Enumerable.Contains(ids, value);
            }
            """;

        Assert.Empty(await AnalyzeAsync(source));
    }

    [Fact]
    public async Task ArrayContainsOutsideExpressionTreeDoesNotReportDiagnosticAsync() {
        const string source = """
            using System;

            public static class Example {
                public static bool Contains(int[] ids, int value) => ids.Contains(value);
            }
            """;

        Assert.Empty(await AnalyzeAsync(source));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source) {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        IEnumerable<MetadataReference> references = new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Expression<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.ExtensionAttribute).Assembly.Location),
        }.DistinctBy(reference => reference.Display, StringComparer.Ordinal);
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [ExpressionTreeSpanOverloadAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        return await compilation
            .WithAnalyzers([new ExpressionTreeSpanOverloadAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
