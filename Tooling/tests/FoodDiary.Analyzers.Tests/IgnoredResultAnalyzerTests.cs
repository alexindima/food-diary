using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class IgnoredResultAnalyzerTests {
    [Theory]
    [InlineData("Read();")]
    [InlineData("await ReadAsync();")]
    [InlineData("await ReadAsync().ConfigureAwait(false);")]
    [InlineData("await ReadValueAsync().ConfigureAwait(false);")]
    [InlineData("ReadAsync();")]
    [InlineData("ReadAsync().ConfigureAwait(false);")]
    [InlineData("_ = Read();")]
    [InlineData("_ = await ReadAsync();")]
    [InlineData("_ = ReadValueAsync();")]
    public async Task DiscardedFailureIsReportedAsync(string body) {
        Diagnostic diagnostic = Assert.Single(await AnalyzeAsync(body));
        Assert.Equal(IgnoredResultAnalyzer.DiagnosticId, diagnostic.Id);
    }

    [Theory]
    [InlineData("return await ReadAsync();")]
    [InlineData("var result = await ReadAsync(); if (result.IsFailure) return result;")]
    [InlineData("Consume(Read());")]
    [InlineData("await Task.Delay(1);")]
    [InlineData("await Task.FromResult(1);")]
    [InlineData("new Other.Result();")]
    [InlineData("#pragma warning disable FD0019 // Deliberate best-effort probe\nRead();\n#pragma warning restore FD0019")]
    public async Task HandledOrUnrelatedResultsAreAllowedAsync(string body) {
        Assert.Empty(await AnalyzeAsync(body));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body) {
        string source = """
            using System.Threading.Tasks;
            using FoodDiary.Results;
            public class Probe {
                public async Task<Result> Run() {
            """ + "\n" + body + "\n" + """
                    return new Result<int>();
                }
                private Result<int> Read() => new();
                private Task<Result<int>> ReadAsync() => Task.FromResult(Read());
                private ValueTask<Result<int>> ReadValueAsync() => new(Read());
                private void Consume(Result result) { }
            }
            namespace FoodDiary.Results {
                public abstract class Result { public bool IsFailure => false; }
                public sealed class Result<T> : Result { }
            }
            namespace Other { public class Result { } }
            """;
        MetadataReference[] references = [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("ResultProbe", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [IgnoredResultAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return await compilation.WithAnalyzers([new IgnoredResultAnalyzer()]).GetAnalyzerDiagnosticsAsync();
    }
}
