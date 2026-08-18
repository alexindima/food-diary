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

    [Fact]
    public void ReverseProxyAccessLogs_DoNotPersistQueryStrings() {
        string nginxConfiguration = ReadSource("nginx.conf");
        string logFormat = Assert.Single(
            nginxConfiguration.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            static line => line.StartsWith("log_format fooddiary_privacy ", StringComparison.Ordinal));
        string[] configurationPaths = [
            ArchitectureTestPaths.FromRoot("nginx.conf"),
            .. Directory.GetFiles(ArchitectureTestPaths.FromRoot("nginx", "sites-enabled"), "*", SearchOption.TopDirectoryOnly),
        ];
        string[] accessLogs = [.. configurationPaths
            .SelectMany(File.ReadAllLines)
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith("access_log ", StringComparison.Ordinal))];

        Assert.NotEmpty(accessLogs);
        Assert.Multiple(
            () => Assert.Contains("\"$request_method $uri $server_protocol\"", logFormat, StringComparison.Ordinal),
            () => Assert.DoesNotContain("$request_uri", logFormat, StringComparison.Ordinal),
            () => Assert.DoesNotContain("$query_string", logFormat, StringComparison.Ordinal),
            () => Assert.DoesNotContain("$args", logFormat, StringComparison.Ordinal),
            () => Assert.DoesNotContain("\"$request\"", logFormat, StringComparison.Ordinal),
            () => Assert.All(accessLogs, static line =>
                Assert.True(
                    string.Equals(line, "access_log off;", StringComparison.Ordinal) ||
                    line.EndsWith(" fooddiary_privacy;", StringComparison.Ordinal),
                    line)));
    }

    [Fact]
    public void PrimaryReverseProxy_RejectsUnknownHostsAndCanonicalizesForwardedHost() {
        string nginx = ReadSource("nginx/sites-enabled/fooddiary.club");

        Assert.Multiple(
            () => Assert.Contains("listen 80 default_server;", nginx, StringComparison.Ordinal),
            () => Assert.Contains("listen 443 ssl default_server;", nginx, StringComparison.Ordinal),
            () => Assert.Contains("listen 443 quic reuseport default_server;", nginx, StringComparison.Ordinal),
            () => Assert.Contains("ssl_reject_handshake on;", nginx, StringComparison.Ordinal),
            () => Assert.Equal(2, CountOccurrences(nginx, "proxy_set_header Host $server_name;")),
            () => Assert.Equal(2, CountOccurrences(nginx, "proxy_set_header X-Forwarded-Host $server_name;")),
            () => Assert.DoesNotContain("proxy_set_header Host $host;", nginx, StringComparison.Ordinal));
    }

    [Fact]
    public void ApiExceptionLogs_UseBoundedRouteLabels() {
        string exceptionHandler = ReadSource("FoodDiary.Web.Api/Extensions/ApiExceptionHandler.cs");

        Assert.Multiple(
            () => Assert.Contains("TelemetryPrivacyProcessor.ResolveRouteLabel(httpContext)", exceptionHandler, StringComparison.Ordinal),
            () => Assert.DoesNotContain("httpContext.Request.Path);", exceptionHandler, StringComparison.Ordinal));
    }

    private static string ReadSource(string path) =>
        File.ReadAllText(ArchitectureTestPaths.FromRoot(path.Split('/')));

    private static int CountOccurrences(string value, string search) {
        int count = 0;
        int offset = 0;
        while ((offset = value.IndexOf(search, offset, StringComparison.Ordinal)) >= 0) {
            count++;
            offset += search.Length;
        }

        return count;
    }

    private static string RelativeLocation(string repositoryRoot, string path, SyntaxNode node) {
        FileLinePositionSpan lineSpan = node.GetLocation().GetLineSpan();
        return string.Concat(
            Path.GetRelativePath(repositoryRoot, path),
            ":",
            (lineSpan.StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture));
    }
}
