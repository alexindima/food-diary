using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class DictionaryCollectionExpressionCompatibilityAnalyzerTests {
    [Theory]
    [InlineData("Dictionary<string, string>", "choose ? existing : [with(StringComparer.Ordinal)]", 1)]
    [InlineData("Dictionary<string, string>", "choose ? ([with(StringComparer.Ordinal)]) : existing", 1)]
    [InlineData("Dictionary<string, string>", "choose ? [with(StringComparer.Ordinal)] : [with(StringComparer.OrdinalIgnoreCase)]", 2)]
    [InlineData("IDictionary<string, string>", "choose ? existing : [with(StringComparer.Ordinal)]", 1)]
    [InlineData("IReadOnlyDictionary<string, string>", "choose ? existing : [with(StringComparer.Ordinal)]", 1)]
    public async Task DictionaryCollectionExpressionInConditionalReportsDiagnosticAsync(
        string targetType,
        string expression,
        int expectedCount) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(targetType, expression);

        Assert.Equal(expectedCount, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => {
            Assert.Equal(DictionaryCollectionExpressionCompatibilityAnalyzer.DiagnosticId, diagnostic.Id);
            Assert.Equal("[", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
        });
    }

    [Theory]
    [InlineData("Echo([with(StringComparer.Ordinal)])")]
    [InlineData("Echo(value: ([with(StringComparer.Ordinal)]))")]
    [InlineData("new Sample([with(StringComparer.Ordinal)]).Value")]
    [InlineData("new Sample(existing)[[with(StringComparer.Ordinal)]]")]
    [InlineData("echo([with(StringComparer.Ordinal)])")]
    public async Task DictionaryCollectionExpressionAsArgumentReportsDiagnosticAsync(string expression) {
        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync("Dictionary<string, string>", expression));

        Assert.Equal(DictionaryCollectionExpressionCompatibilityAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("argument list", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Dictionary<string, string>", "[with(StringComparer.Ordinal)]")]
    [InlineData("Dictionary<string, string>", "choose ? existing : []")]
    [InlineData("Dictionary<string, string>", "choose ? existing : new(StringComparer.Ordinal)")]
    [InlineData("IReadOnlyDictionary<string, string>", "choose ? existing : new Dictionary<string, string>(StringComparer.Ordinal)")]
    [InlineData("HashSet<string>", "choose ? new HashSet<string>() : [with(StringComparer.Ordinal)]")]
    [InlineData("List<string>", "choose ? new List<string>() : [with(capacity: 4)]")]
    [InlineData("List<string>", """choose ? new List<string>() : [nameof(with)]""")]
    [InlineData("Dictionary<string, string>", "Echo(existing)")]
    [InlineData("Dictionary<string, string>", "Echo([])")]
    [InlineData("Dictionary<string, string>", "Echo(new Dictionary<string, string>(StringComparer.Ordinal))")]
    [InlineData("HashSet<string>", "EchoSet([with(StringComparer.Ordinal)])")]
    public async Task UnaffectedCollectionExpressionDoesNotReportDiagnosticAsync(string targetType, string expression) {
        Assert.Empty(await AnalyzeAsync(targetType, expression));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string targetType, string expression) {
        string source = $$"""
            using System;
            using System.Collections.Generic;

            sealed class Sample(Dictionary<string, string> value) {
                public Dictionary<string, string> Value => value;
                public Dictionary<string, string> this[Dictionary<string, string> key] => key;
                private static Dictionary<string, string> Echo(Dictionary<string, string> value) => value;
                private static HashSet<string> EchoSet(HashSet<string> value) => value;

                public static {{targetType}} Create(bool choose, Dictionary<string, string> existing, int with) {
                    Func<Dictionary<string, string>, Dictionary<string, string>> echo = Echo;
                    {{targetType}} result = {{expression}};
                    return result;
                }
            }
            """;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [DictionaryCollectionExpressionCompatibilityAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        return await compilation
            .WithAnalyzers([new DictionaryCollectionExpressionCompatibilityAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
