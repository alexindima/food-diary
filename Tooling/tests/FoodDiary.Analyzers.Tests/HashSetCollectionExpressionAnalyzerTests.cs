using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class HashSetCollectionExpressionAnalyzerTests {
    [Theory]
    [InlineData("""HashSet<string> set = new(["Admin", Support], StringComparer.Ordinal);""")]
    [InlineData("""HashSet<string> set = new HashSet<string>(["Admin"], StringComparer.Ordinal);""")]
    [InlineData("HashSet<string> set = new(Enum.GetNames<DayOfWeek>(), StringComparer.OrdinalIgnoreCase);")]
    [InlineData("""StringSet set = new(comparer: StringComparer.OrdinalIgnoreCase, collection: ["Admin"]);""")]
    [InlineData("HashSet<string> set = new(names, StringComparer.Ordinal);")]
    [InlineData("HashSet<string> set = new(source, StringComparer.Ordinal);")]
    [InlineData("HashSet<string> set = new(sourceSet, StringComparer.OrdinalIgnoreCase);")]
    [InlineData("""HashSet<string> set = new(["Admin", .. names], StringComparer.Ordinal);""")]
    [InlineData("HashSet<string> set = new([names[0]], StringComparer.Ordinal);")]
    [InlineData("HashSet<string> set = new(Other.GetNames<DayOfWeek>(), StringComparer.Ordinal);")]
    public async Task EnumerableAndOrdinalComparerReportDiagnosticAsync(string statement) {
        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(statement));

        Assert.Multiple(
            () => Assert.Equal(HashSetCollectionExpressionAnalyzer.DiagnosticId, diagnostic.Id),
            () => Assert.Equal("new", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan)));
    }

    [Theory]
    [InlineData("""HashSet<string> set = [with(StringComparer.Ordinal), "Admin"];""")]
    [InlineData("HashSet<string> set = [with(source, StringComparer.Ordinal)];")]
    [InlineData("""HashSet<string> set = new(["Admin"], StringComparer.CurrentCulture);""")]
    [InlineData("""HashSet<string> set = new(["Admin"], comparer);""")]
    [InlineData("""HashSet<string> set = new(["Admin"], Other.Ordinal);""")]
    [InlineData("HashSet<string> set = new(10, StringComparer.Ordinal);")]
    [InlineData("""var set = new HashSet<string>(["Admin"], StringComparer.Ordinal);""")]
    [InlineData("""IEnumerable<string> set = new HashSet<string>(["Admin"], StringComparer.Ordinal);""")]
    [InlineData("""List<string> set = new(["Admin"]);""")]
    public async Task OtherCollectionCreationsDoNotReportDiagnosticAsync(string statement) {
        Assert.Empty(await AnalyzeAsync(statement));
    }

    [Fact]
    public async Task CSharp14DoesNotReportDiagnosticAsync() {
        Assert.Empty(await AnalyzeAsync(
            """HashSet<string> set = new(["Admin"], StringComparer.Ordinal);""",
            LanguageVersion.CSharp14));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string statement,
        LanguageVersion languageVersion = LanguageVersion.Preview) {
        string source = $$"""
            using System;
            using System.Collections.Generic;
            using StringSet = System.Collections.Generic.HashSet<string>;

            static class Sample {
                private const string Support = "Support";

                private static void Create(
                    string[] names,
                    IEnumerable<string> source,
                    HashSet<string> sourceSet,
                    StringComparer comparer) {
                    {{statement}}
                    _ = set;
                }
            }

            static class Other {
                public static StringComparer Ordinal => StringComparer.Ordinal;
                public static string[] GetNames<T>() => [];
            }
            """;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(languageVersion));
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [HashSetCollectionExpressionAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));

        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        return await compilation
            .WithAnalyzers([new HashSetCollectionExpressionAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
    }
}
