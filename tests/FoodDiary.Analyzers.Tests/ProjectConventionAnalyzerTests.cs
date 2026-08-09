using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProjectConventionAnalyzerTests {
    [Theory]
    [InlineData("public Task Load(CancellationToken cancellationToken) => Task.CompletedTask;", ProjectConventionAnalyzer.AsyncSuffixRequiredId)]
    [InlineData("public string LoadAsync() => string.Empty;", ProjectConventionAnalyzer.AsyncSuffixForbiddenId)]
    [InlineData("public Task LoadAsync() => Task.CompletedTask;", ProjectConventionAnalyzer.CancellationTokenRequiredId)]
    public async Task InvalidMethodConventionReportsDiagnosticAsync(string method, string diagnosticId) {
        string source = $$"""
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class Service {
                {{method}}
            }
            """;

        Assert.Contains(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(diagnostic.Id, diagnosticId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidAsyncMethodDoesNotReportMethodConventionDiagnosticsAsync() {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class Service {
                public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """;

        Diagnostic[] diagnostics = await AnalyzeAsync(source);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id is
            ProjectConventionAnalyzer.AsyncSuffixRequiredId or
            ProjectConventionAnalyzer.AsyncSuffixForbiddenId or
            ProjectConventionAnalyzer.CancellationTokenRequiredId);
    }

    [Fact]
    public async Task TargetTypedNewInvocationArgumentReportsDiagnosticAsync() {
        const string source = """
            public sealed class Item;

            public sealed class Service {
                public void Run() => Consume(new());
                private static void Consume(Item item) { }
            }
            """;

        Assert.Contains(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.ExplicitInvocationArgumentTypeId,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task DirectUtcNowReportsDiagnosticAsync() {
        const string source = """
            using System;

            public sealed class Clock {
                public DateTime Now => DateTime.UtcNow;
            }
            """;

        Assert.Contains(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.TimeProviderRequiredId,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task UserDefinedUtcNowDoesNotReportDiagnosticAsync() {
        const string source = """
            public sealed class ClockValue {
                public static int UtcNow => 42;
            }

            public sealed class Consumer {
                public int Now => ClockValue.UtcNow;
            }
            """;

        Assert.DoesNotContain(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.TimeProviderRequiredId,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task TestTypeWithoutCoverageExclusionReportsDiagnosticAsync() {
        const string source = "public sealed class ExampleTests { }";

        Assert.Contains(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.TestCoverageExclusionRequiredId,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task AttributedPartialTestTypeDoesNotReportCoverageDiagnosticAsync() {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            [ExcludeFromCodeCoverage]
            public sealed partial class ExampleTests { }
            public sealed partial class ExampleTests { }
            """;

        Assert.DoesNotContain(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.TestCoverageExclusionRequiredId,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnsealedConcreteClassReportsDiagnosticAsync() {
        const string source = "public class Service { }";

        Assert.Contains(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.ConcreteClassMustBeClosedId,
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("public sealed class Service { }")]
    [InlineData("public static class Service { }")]
    [InlineData("public abstract class Service { }")]
    public async Task ClassClosedForInheritanceDoesNotReportDiagnosticAsync(string source) {
        Assert.DoesNotContain(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.ConcreteClassMustBeClosedId,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExternalTestConnectionReportsDiagnosticAsync() {
        const string source = """
            public sealed class NetworkTests {
                public void Run(dynamic client) => client.ConnectAsync("example.com", 443);
            }
            """;

        Assert.Contains(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.ExternalTestConnectionForbiddenId,
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("::1")]
    [InlineData("127.0.0.1")]
    public async Task LoopbackTestConnectionDoesNotReportDiagnosticAsync(string host) {
        string source = $$"""
            public sealed class NetworkTests {
                public void Run(dynamic client) => client.ConnectAsync("{{host}}", 443);
            }
            """;

        Assert.DoesNotContain(
            await AnalyzeAsync(source),
            diagnostic => string.Equals(
                diagnostic.Id,
                ProjectConventionAnalyzer.ExternalTestConnectionForbiddenId,
                StringComparison.Ordinal));
    }

    private static async Task<Diagnostic[]> AnalyzeAsync(string source) {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.CSharp14),
            path: "Project/Source.cs");
        string[] trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<MetadataReference> references = trustedPlatformAssemblies
            .Select(static path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>(StringComparer.Ordinal) {
                    [ProjectConventionAnalyzer.AsyncSuffixRequiredId] = ReportDiagnostic.Error,
                    [ProjectConventionAnalyzer.AsyncSuffixForbiddenId] = ReportDiagnostic.Error,
                    [ProjectConventionAnalyzer.CancellationTokenRequiredId] = ReportDiagnostic.Error,
                    [ProjectConventionAnalyzer.ExplicitInvocationArgumentTypeId] = ReportDiagnostic.Error,
                    [ProjectConventionAnalyzer.TimeProviderRequiredId] = ReportDiagnostic.Error,
                    [ProjectConventionAnalyzer.TestCoverageExclusionRequiredId] = ReportDiagnostic.Error,
                    [ProjectConventionAnalyzer.ConcreteClassMustBeClosedId] = ReportDiagnostic.Error,
                    [ProjectConventionAnalyzer.ExternalTestConnectionForbiddenId] = ReportDiagnostic.Error,
                }));

        return [.. await compilation
            .WithAnalyzers([new ProjectConventionAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false)];
    }
}
