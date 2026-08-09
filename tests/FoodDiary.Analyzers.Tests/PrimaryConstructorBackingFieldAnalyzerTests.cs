using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class PrimaryConstructorBackingFieldAnalyzerTests {
    [Fact]
    public async Task ReadonlyFieldInitializedFromPrimaryConstructorParameterReportsDiagnosticAsync() {
        const string source = """
            public sealed class Service(string value) {
                private readonly string _value = value;
                public string GetValue() => _value;
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(source));

        Assert.Multiple(
            () => Assert.Equal(PrimaryConstructorBackingFieldAnalyzer.DiagnosticId, diagnostic.Id),
            () => Assert.Contains("use the parameter directly", diagnostic.GetMessage(), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("public sealed class Service(string value) { private string _value = value; }")]
    [InlineData("public sealed class Service(string value) { private readonly string _value = value.Trim(); }")]
    [InlineData("public sealed class Service { private readonly string _value = \"value\"; }")]
    public async Task OtherFieldsDoNotReportDiagnosticAsync(string source) {
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
                    [PrimaryConstructorBackingFieldAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        return await compilation
            .WithAnalyzers([new PrimaryConstructorBackingFieldAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
