using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class ReadModelWriteBoundaryAnalyzerTests {
    [Theory]
    [InlineData("query.ExecuteDelete();")]
    [InlineData("Action<IQueryable<int>> write = EntityFrameworkQueryableExtensions.ExecuteDelete<int>;")]
    [InlineData("query.AsTracking();")]
    [InlineData("query.FromSqlRaw();")]
    [InlineData("db.SaveChanges();")]
    [InlineData("Action save = db.SaveChanges;")]
    [InlineData("var tracker = db.ChangeTracker;")]
    [InlineData("var context = (DbContext)unknown;")]
    [InlineData("command.ExecuteNonQuery();")]
    [InlineData("Func<int> write = command.ExecuteNonQuery;")]
    [InlineData("transaction.Commit();")]
    public async Task ComposedReadsCannotAcquireWriteCapabilitiesAsync(string body) {
        Assert.NotEmpty(await AnalyzeAsync(body, "FoodDiary.ReadModel.Composition"));
        Assert.Empty(await AnalyzeAsync(body, "FoodDiary.Modules.Products.Infrastructure"));
    }

    [Fact]
    public async Task NoTrackingProjectionIsAllowedAsync() {
        Assert.Empty(await AnalyzeAsync("var rows = query.AsNoTracking().Where(value => value > 0).ToArray();",
            "FoodDiary.ReadModel.Composition"));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body, string assembly) {
        string source = """
            using System;
            using System.Linq;
            using System.Data.Common;
            using Microsoft.EntityFrameworkCore;
            public class Probe {
                public void Run(IQueryable<int> query, DbContext db, object unknown, DbCommand command, DbTransaction transaction) {
            """ + body + """
                }
            }
            namespace Microsoft.EntityFrameworkCore {
                public class DbContext {
                    public object ChangeTracker => null;
                    public void SaveChanges() { }
                }
                public static class EntityFrameworkQueryableExtensions {
                    public static void ExecuteDelete<T>(this IQueryable<T> source) { }
                    public static IQueryable<T> AsTracking<T>(this IQueryable<T> source) => source;
                    public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> source) => source;
                    public static IQueryable<T> FromSqlRaw<T>(this IQueryable<T> source) => source;
                }
            }
            """;
        MetadataReference[] references = [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create(assembly, [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [ReadModelWriteBoundaryAnalyzer.DiagnosticId] = ReportDiagnostic.Error,
                }));
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return await compilation.WithAnalyzers([new ReadModelWriteBoundaryAnalyzer()]).GetAnalyzerDiagnosticsAsync();
    }
}
