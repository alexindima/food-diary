using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModulePersistenceBoundaryAnalyzerTests {
    private const string SourcePath = "C:/FD/Modules/Products/Infrastructure/Probe.cs";

    [Theory]
    [InlineData("db.Users.Add(new User());")]
    [InlineData("Action<User> add = db.Users.Add;")]
    [InlineData("db.AddRange(new User[] { new User() });")]
    [InlineData("db.AddRange(new List<User> { new User() });")]
    [InlineData("db.Add(new User());")]
    [InlineData("Action<object> add = db.Add; add(new User());")]
    [InlineData("Action<User> entry = db.Entry<User>; entry(new User());")]
    [InlineData("Func<DbSet<User>> set = db.Set<User>; set();")]
    [InlineData("object user = new User(); db.Add(user);")]
    [InlineData("db.Entry(new User());")]
    [InlineData("db.Set<User>();")]
    [InlineData("db.Find<User>(1);")]
    [InlineData("db.Users.AsNoTracking().ExecuteDelete();")]
    [InlineData("var query = db.Users.AsNoTracking(); query.ExecuteUpdate();")]
    [InlineData("var query = db.Users.AsNoTracking(); query.AsTracking();")]
    [InlineData("var query = db.Users.Where(user => true); query.ToList();")]
    public async Task ForeignWriteOrTrackingReportsDiagnosticAsync(string body) {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(body);
        Assert.Contains(diagnostics, diagnostic => string.Equals(diagnostic.Id, ModulePersistenceBoundaryAnalyzer.OwnershipDiagnosticId, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("db.Products.Add(new Product());")]
    [InlineData("Action<Product> add = db.Products.Add;")]
    [InlineData("Func<int, Product> find = db.Find<Product>;")]
    [InlineData("Action action = () => { }; action();")]
    [InlineData("db.Add(new Product());")]
    [InlineData("db.Entry(new Product());")]
    [InlineData("Action<Product> entry = db.Entry<Product>; entry(new Product());")]
    [InlineData("db.Set<Product>();")]
    [InlineData("db.Users.AsNoTracking().Where(user => true).ToList();")]
    [InlineData("db.Set<User>().AsNoTracking().ToList();")]
    [InlineData("EntityFrameworkQueryableExtensions.AsNoTracking(db.Users).ToList();")]
    public async Task OwnedWritesAndExplicitForeignReadsAreAllowedAsync(string body) =>
        Assert.Empty(await AnalyzeAsync(body));

    [Fact]
    public async Task TechnicalEscapeWithoutReviewReportsDiagnosticAsync() {
        Assert.Contains(await AnalyzeAsync("db.SaveChanges();"),
            diagnostic => string.Equals(diagnostic.Id, ModulePersistenceBoundaryAnalyzer.TechnicalDiagnosticId, StringComparison.Ordinal));
        Assert.Contains(await AnalyzeAsync("var tracker = db.ChangeTracker;"),
            diagnostic => string.Equals(diagnostic.Id, ModulePersistenceBoundaryAnalyzer.TechnicalDiagnosticId, StringComparison.Ordinal));
        Assert.Contains(await AnalyzeAsync("Func<int> save = db.SaveChanges; save();"),
            diagnostic => string.Equals(diagnostic.Id, ModulePersistenceBoundaryAnalyzer.TechnicalDiagnosticId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReviewedTechnicalSourceAllowsOnlyExactBodyAsync() {
        Assert.Empty(await AnalyzeAsync("db.SaveChanges();", reviewedBody: "db.SaveChanges();"));
        Assert.Contains(await AnalyzeAsync("db.SaveChanges(); db.SaveChanges();", reviewedBody: "db.SaveChanges();"),
            diagnostic => string.Equals(diagnostic.Id, ModulePersistenceBoundaryAnalyzer.TechnicalDiagnosticId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task TechnicalReviewDoesNotAllowForeignWritesAsync() {
        Assert.Contains(await AnalyzeAsync("db.Add(new User());", reviewedBody: "db.Add(new User());"),
            diagnostic => string.Equals(diagnostic.Id, ModulePersistenceBoundaryAnalyzer.OwnershipDiagnosticId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SharedAuditWritesRequireExactReviewAsync() {
        const string body = "db.Set<AuditRecord>().Add(new AuditRecord());";
        Assert.Contains(await AnalyzeAsync(body), diagnostic => string.Equals(diagnostic.Id, ModulePersistenceBoundaryAnalyzer.OwnershipDiagnosticId, StringComparison.Ordinal));
        Assert.Empty(await AnalyzeAsync(body, reviewedBody: body));
        Assert.Contains(await AnalyzeAsync(body + " db.SaveChanges();", reviewedBody: body), diagnostic => string.Equals(diagnostic.Id, ModulePersistenceBoundaryAnalyzer.OwnershipDiagnosticId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task NonModuleCompositionRootIsOutsideScopeAsync() =>
        Assert.Empty(await AnalyzeAsync("db.Add(new User()); db.SaveChanges();", assembly: "FoodDiary.Web.Api"));

    [Fact]
    public async Task Fingerprints_IgnoreMalformedNonModuleAndDuplicateEntries() {
        const string body = "db.SaveChanges();";
        string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Source(body).Trim())));
        const string path = "Modules/Products/Infrastructure/Probe.cs";
        string content = $"\n# comment\ninvalid\n{hash} Services/Probe.cs\n{hash} {path}\n{new string('0', 64)} {path}\n";
        Assert.Empty(await AnalyzeAsync(body, fingerprintContent: content));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string body, string? reviewedBody = null, string assembly = "FoodDiary.Modules.Products.Infrastructure", string? fingerprintContent = null) {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path)).Cast<MetadataReference>().ToList();
        foreach (string owner in new[] { "Users", "Products" }) {
            string entity = string.Equals(owner, "Users", StringComparison.Ordinal) ? "User" : "Product";
            var domain = CSharpCompilation.Create("FoodDiary.Modules." + owner + ".Domain",
                [CSharpSyntaxTree.ParseText("public sealed class " + entity + " { }")], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            references.Add(domain.ToMetadataReference());
        }
        var audit = CSharpCompilation.Create("FoodDiary.Audit.PersistenceModel",
            [CSharpSyntaxTree.ParseText("public sealed class AuditRecord { }")], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        references.Add(audit.ToMetadataReference());
        var compilation = CSharpCompilation.Create(assembly,
            [CSharpSyntaxTree.ParseText(Source(body), path: SourcePath)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [ModulePersistenceBoundaryAnalyzer.OwnershipDiagnosticId] = ReportDiagnostic.Error,
                    [ModulePersistenceBoundaryAnalyzer.TechnicalDiagnosticId] = ReportDiagnostic.Error,
                }));
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        ImmutableArray<AdditionalText> files = [];
        if (reviewedBody is not null) {
            string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Source(reviewedBody).Trim())));
            files = [new InMemoryAdditionalText("C:/FD/docs/architecture/persistence-technical-sources.txt",
                hash + " Modules/Products/Infrastructure/Probe.cs")];
        }
        if (fingerprintContent is not null) {
            files = [new InMemoryAdditionalText("C:/FD/docs/architecture/persistence-technical-sources.txt", fingerprintContent)];
        }
        return await compilation.WithAnalyzers([new ModulePersistenceBoundaryAnalyzer()], new AnalyzerOptions(files))
            .GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    private static string Source(string body) => """
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using System.Linq.Expressions;
        using Microsoft.EntityFrameworkCore;
        public sealed class Probe {
            public void Run(Database db) {
        """ + body + """
            }
        }
        public sealed class Database : DbContext {
            public DbSet<User> Users => new();
            public DbSet<Product> Products => new();
        }
        namespace Microsoft.EntityFrameworkCore {
            public class DbContext {
                public object ChangeTracker => new();
                public DbSet<T> Set<T>() => new();
                public void Add(object value) { }
                public void AddRange(User[] values) { }
                public void AddRange(List<User> values) { }
                public void Entry<T>(T value) { }
                public T Find<T>(int id) => default!;
                public int SaveChanges() => 0;
            }
            public class DbSet<TEntity> : IQueryable<TEntity> {
                public void Add(TEntity value) { }
                public Type ElementType => typeof(TEntity);
                public Expression Expression => throw new NotImplementedException();
                public IQueryProvider Provider => throw new NotImplementedException();
                public IEnumerator<TEntity> GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            public static class EntityFrameworkQueryableExtensions {
                public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> query) => query;
                public static IQueryable<T> AsTracking<T>(this IQueryable<T> query) => query;
                public static void ExecuteDelete<T>(this IQueryable<T> query) { }
                public static void ExecuteUpdate<T>(this IQueryable<T> query) { }
            }
        }
        """;

    private sealed class InMemoryAdditionalText(string path, string contents) : AdditionalText {
        public override string Path => path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(contents);
    }
}
