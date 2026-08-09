using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class PrivateInstanceFieldNamingAnalyzerTests {
    [Fact]
    public async Task PrivateInstanceFieldWithoutUnderscoreReportsDiagnosticAsync() {
        const string source = "public sealed class Service { private readonly string value = string.Empty; }";

        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(source));

        Assert.Equal(PrivateInstanceFieldNamingAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Theory]
    [InlineData("public sealed class Service { private readonly string _value = string.Empty; }")]
    [InlineData("public sealed class Service { private static readonly string Value = string.Empty; }")]
    [InlineData("public sealed class Service { private const string Value = \"value\"; }")]
    public async Task ValidOrNonInstanceFieldsDoNotReportDiagnosticAsync(string source) {
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
                    [PrivateInstanceFieldNamingAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        return await compilation
            .WithAnalyzers([new PrivateInstanceFieldNamingAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
