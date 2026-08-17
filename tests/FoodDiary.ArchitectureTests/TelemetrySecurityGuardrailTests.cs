using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class TelemetrySecurityGuardrailTests {
    private static readonly string[] TelemetryProjectPaths = [
        "FoodDiary.Web.Api/FoodDiary.Web.Api.csproj",
        "FoodDiary.JobManager/FoodDiary.JobManager.csproj",
        "MailRelay/FoodDiary.MailRelay.Infrastructure/FoodDiary.MailRelay.Infrastructure.csproj",
        "MailInbox/FoodDiary.MailInbox.Infrastructure/FoodDiary.MailInbox.Infrastructure.csproj",
    ];

    private static readonly string[] TelemetrySourceRoots = [
        "FoodDiary.Web.Api",
        "FoodDiary.Presentation.Api",
        "FoodDiary.JobManager",
        "MailRelay/FoodDiary.MailRelay.Application",
        "MailRelay/FoodDiary.MailRelay.Infrastructure",
        "MailRelay/FoodDiary.MailRelay.Presentation",
        "MailInbox/FoodDiary.MailInbox.Application",
        "MailInbox/FoodDiary.MailInbox.Infrastructure",
        "MailInbox/FoodDiary.MailInbox.Presentation",
    ];

    [Fact]
    public void RuntimeHosts_RegisterExpectedAutomaticTraceInstrumentation() {
        string api = ReadSource("FoodDiary.Web.Api/Extensions/ApiTelemetryServiceCollectionExtensions.cs");
        string jobManager = ReadSource("FoodDiary.JobManager/Services/JobManagerTelemetryServiceCollectionExtensions.cs");
        string mailRelay = ReadSource("MailRelay/FoodDiary.MailRelay.Infrastructure/Extensions/MailRelayServiceCollectionExtensions.cs");
        string mailInbox = ReadSource("MailInbox/FoodDiary.MailInbox.Infrastructure/Extensions/MailInboxServiceCollectionExtensions.cs");

        Assert.Multiple(
            () => Assert.Contains(".AddAspNetCoreInstrumentation(", api, StringComparison.Ordinal),
            () => Assert.Contains(".AddHttpClientInstrumentation(", api, StringComparison.Ordinal),
            () => Assert.Contains(".AddNpgsql()", api, StringComparison.Ordinal),
            () => Assert.Contains(".AddHttpClientInstrumentation(", jobManager, StringComparison.Ordinal),
            () => Assert.Contains(".AddNpgsql()", jobManager, StringComparison.Ordinal),
            () => Assert.Contains(".AddAspNetCoreInstrumentation(", mailRelay, StringComparison.Ordinal),
            () => Assert.Contains(".AddHttpClientInstrumentation(", mailRelay, StringComparison.Ordinal),
            () => Assert.Contains(".AddNpgsql()", mailRelay, StringComparison.Ordinal),
            () => Assert.Contains(".AddAspNetCoreInstrumentation(", mailInbox, StringComparison.Ordinal),
            () => Assert.Contains(".AddNpgsql()", mailInbox, StringComparison.Ordinal));

        foreach (string projectPath in TelemetryProjectPaths) {
            string project = ReadSource(projectPath);
            Assert.Contains("<PackageReference Include=\"Npgsql.OpenTelemetry\" />", project, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AutomaticHttpTracing_ExcludesHealthAndUsesPrivacyProcessors() {
        string api = ReadSource("FoodDiary.Web.Api/Extensions/ApiTelemetryServiceCollectionExtensions.cs");
        string apiProcessor = ReadSource("FoodDiary.Web.Api/Extensions/TelemetryPrivacyProcessor.cs");
        string mailRelay = ReadSource("MailRelay/FoodDiary.MailRelay.Infrastructure/Extensions/MailRelayServiceCollectionExtensions.cs");
        string mailInbox = ReadSource("MailInbox/FoodDiary.MailInbox.Infrastructure/Extensions/MailInboxServiceCollectionExtensions.cs");

        Assert.Multiple(
            () => Assert.Contains("options.Filter = TelemetryPrivacyProcessor.ShouldCollectRequest", api, StringComparison.Ordinal),
            () => Assert.Contains("StartsWithSegments(\"/health\"", apiProcessor, StringComparison.Ordinal),
            () => Assert.Contains(".AddProcessor(new TelemetryPrivacyProcessor())", api, StringComparison.Ordinal),
            () => Assert.Contains(".AddProcessor(new MailRelayTelemetryPrivacyProcessor())", mailRelay, StringComparison.Ordinal),
            () => Assert.Contains(".AddProcessor(new MailInboxTelemetryPrivacyProcessor())", mailInbox, StringComparison.Ordinal));
    }

    [Fact]
    public void TelemetryActivities_DoNotAttachRawIdentityOrErrorMessages() {
        string repositoryRoot = ArchitectureTestPaths.RepositoryRoot;
        List<string> violations = [];

        foreach (string sourceRoot in TelemetrySourceRoots) {
            string absoluteRoot = ArchitectureTestPaths.FromRoot(sourceRoot.Split('/'));
            foreach (string path in SourceScanner.SourceFiles(absoluteRoot)) {
                SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
                foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
                    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) {
                        continue;
                    }

                    if (string.Equals(memberAccess.Name.Identifier.ValueText, "SetTag", StringComparison.Ordinal) &&
                        invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literal &&
                        literal.Token.ValueText is "enduser.id" or "error.message") {
                        violations.Add(RelativeLocation(repositoryRoot, path, invocation));
                    }

                    if (string.Equals(memberAccess.Name.Identifier.ValueText, "SetStatus", StringComparison.Ordinal) &&
                        invocation.ArgumentList.Arguments.Skip(1).Any(argument =>
                            argument.Expression.DescendantNodesAndSelf()
                                .OfType<MemberAccessExpressionSyntax>()
                                .Any(access => string.Equals(access.Name.Identifier.ValueText, "Message", StringComparison.Ordinal)))) {
                        violations.Add(RelativeLocation(repositoryRoot, path, invocation));
                    }
                }
            }
        }

        Assert.Empty(violations.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void RequestMetrics_UseBoundedRouteLabels() {
        string middleware = ReadSource("FoodDiary.Web.Api/Extensions/RequestObservabilityMiddleware.cs");
        string rateLimiter = ReadSource("FoodDiary.Web.Api/Options/RateLimiterOptionsSetup.cs");
        string processor = ReadSource("FoodDiary.Web.Api/Extensions/TelemetryPrivacyProcessor.cs");

        Assert.Multiple(
            () => Assert.Contains("TelemetryPrivacyProcessor.ResolveRouteLabel(context)", middleware, StringComparison.Ordinal),
            () => Assert.Contains("TelemetryPrivacyProcessor.ResolveRouteLabel(httpContext)", rateLimiter, StringComparison.Ordinal),
            () => Assert.DoesNotContain("httpContext.Request.Path.Value", rateLimiter, StringComparison.Ordinal),
            () => Assert.Contains("public const string UnmatchedRouteLabel = \"unmatched\"", processor, StringComparison.Ordinal));
    }

    private static string ReadSource(string path) =>
        File.ReadAllText(ArchitectureTestPaths.FromRoot(path.Split('/')));

    private static string RelativeLocation(string repositoryRoot, string path, SyntaxNode node) {
        FileLinePositionSpan lineSpan = node.GetLocation().GetLineSpan();
        return string.Concat(
            Path.GetRelativePath(repositoryRoot, path),
            ":",
            (lineSpan.StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture));
    }
}
