using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class ParameterNamingAnalyzerTests {
    [Fact]
    public async Task UppercaseMethodParameterReportsDiagnosticAsync() {
        const string source = "public static class Factory { public static void Create(bool UseNullValue) { } }";

        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(source));

        Assert.Equal(ParameterNamingAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Theory]
    [InlineData("public static class Factory { public static void Create(bool useNullValue) { } }")]
    [InlineData("public sealed record Response(string Value);")]
    public async Task ValidParametersDoNotReportDiagnosticAsync(string source) {
        Assert.Empty(await AnalyzeAsync(source));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source) {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [ParameterNamingAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        return await compilation
            .WithAnalyzers([new ParameterNamingAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
