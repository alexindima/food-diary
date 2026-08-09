using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class RedundantEmptyRecordParameterListAnalyzerTests {
    [Theory]
    [InlineData("public sealed record Request();")]
    [InlineData("public readonly record struct Request();")]
    public async Task EmptyRecordParameterListReportsDiagnosticAsync(string source) {
        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(source));

        Assert.Multiple(
            () => Assert.Equal(RedundantEmptyRecordParameterListAnalyzer.DiagnosticId, diagnostic.Id),
            () => Assert.Equal("Record 'Request' has a redundant empty parameter list", diagnostic.GetMessage()));
    }

    [Theory]
    [InlineData("public sealed record Request;")]
    [InlineData("public sealed record Request(int Page);")]
    [InlineData("public sealed class Request { public Request() { } }")]
    public async Task OtherTypeDeclarationsDoNotReportDiagnosticAsync(string source) {
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
                    [RedundantEmptyRecordParameterListAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        return await compilation
            .WithAnalyzers([new RedundantEmptyRecordParameterListAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
