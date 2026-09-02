using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class UseExtensionBlockAnalyzerTests {
    [Fact]
    public async Task ClassicExtensionMethodReportsDiagnosticAsync() {
        const string source = """
            public static class StringExtensions {
                public static string Echo(this string value) => value;
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(source));

        Assert.Multiple(
            () => Assert.Equal(UseExtensionBlockAnalyzer.DiagnosticId, diagnostic.Id),
            () => Assert.Equal("Convert extension method 'Echo' to a C# 14 extension block", diagnostic.GetMessage()));
    }

    [Fact]
    public async Task ExtensionBlockDoesNotReportDiagnosticAsync() {
        const string source = """
            public static class StringExtensions {
                extension(string value) {
                    public string Echo() => value;
                }
            }
            """;

        Assert.Empty(await AnalyzeAsync(source));
    }

    [Fact]
    public async Task OrdinaryStaticMethodDoesNotReportDiagnosticAsync() {
        const string source = """
            public static class StringHelpers {
                public static string Echo(string value) => value;
            }
            """;

        Assert.Empty(await AnalyzeAsync(source));
    }

    private static async Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> AnalyzeAsync(string source) {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        IEnumerable<MetadataReference> references = new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.ExtensionAttribute).Assembly.Location),
        }.DistinctBy(reference => reference.Display, StringComparer.Ordinal);
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [UseExtensionBlockAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        return await compilation
            .WithAnalyzers([new UseExtensionBlockAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
