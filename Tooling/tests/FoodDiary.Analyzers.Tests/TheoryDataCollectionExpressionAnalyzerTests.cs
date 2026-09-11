using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class TheoryDataCollectionExpressionAnalyzerTests {
    private const string TheoryDataStub = """
        namespace Xunit {
            public sealed class TheoryData<T> : System.Collections.Generic.List<T> { }
            public sealed class TheoryData<T1, T2> : System.Collections.Generic.List<(T1, T2)> {
                public void Add(T1 first, T2 second) => Add((first, second));
            }
        }
        """;

    [Theory]
    [InlineData("Xunit.TheoryData<int> Data => new() { 1, 2 };")]
    [InlineData("Xunit.TheoryData<int> Data => new Xunit.TheoryData<int> { 1, 2 };")]
    public async Task TheoryDataCollectionInitializerReportsDiagnosticAsync(string member) {
        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(member));

        Assert.Multiple(
            () => Assert.Equal(TheoryDataCollectionExpressionAnalyzer.DiagnosticId, diagnostic.Id),
            () => Assert.Equal(
                "Use a collection expression instead of a collection initializer for TheoryData",
                diagnostic.GetMessage()));
    }

    [Theory]
    [InlineData("Xunit.TheoryData<int> Data => [1, 2];")]
    [InlineData("System.Collections.Generic.List<int> Data => new() { 1, 2 };")]
    [InlineData("Other.TheoryData<int> Data => new() { 1, 2 };")]
    [InlineData("Xunit.TheoryData<int, int> Data => new() { { 1, 2 } };")]
    public async Task OtherCollectionCreationDoesNotReportDiagnosticAsync(string member) {
        Assert.Empty(await AnalyzeAsync(member));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string member) {
        string source = $$"""
            {{TheoryDataStub}}

            namespace Other {
                public sealed class TheoryData<T> : System.Collections.Generic.List<T> { }
            }

            public sealed class TestData {
                public {{member}}
            }
            """;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [TheoryDataCollectionExpressionAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        return await compilation
            .WithAnalyzers([new TheoryDataCollectionExpressionAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
