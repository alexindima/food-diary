using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class TelemetrySecurityGuardrailTests {
    private static readonly string[] TelemetryProjectPaths = [
        "FoodDiary.Web.Api/FoodDiary.Web.Api.csproj",
        "FoodDiary.JobManager/FoodDiary.JobManager.csproj",
        "Services/MailRelay/FoodDiary.MailRelay.Infrastructure/FoodDiary.MailRelay.Infrastructure.csproj",
        "Services/MailInbox/FoodDiary.MailInbox.Infrastructure/FoodDiary.MailInbox.Infrastructure.csproj",
    ];

    private static readonly string[] TelemetrySourceRoots = [
        "FoodDiary.Web.Api",
        "FoodDiary.Presentation.Api",
        "FoodDiary.JobManager",
        "Services/MailRelay/FoodDiary.MailRelay.Application",
        "Services/MailRelay/FoodDiary.MailRelay.Infrastructure",
        "Services/MailRelay/FoodDiary.MailRelay.Presentation",
        "Services/MailInbox/FoodDiary.MailInbox.Application",
        "Services/MailInbox/FoodDiary.MailInbox.Infrastructure",
        "Services/MailInbox/FoodDiary.MailInbox.Presentation",
    ];

    [Fact]
    public void RuntimeHosts_RegisterExpectedAutomaticTraceInstrumentation() {
        string api = ReadSource("FoodDiary.Web.Api/Extensions/ApiTelemetryServiceCollectionExtensions.cs");
        string jobManager = ReadSource("FoodDiary.JobManager/Services/JobManagerTelemetryServiceCollectionExtensions.cs");
        string mailRelay = ReadSource("Services/MailRelay/FoodDiary.MailRelay.Infrastructure/Extensions/MailRelayServiceCollectionExtensions.cs");
        string mailInbox = ReadSource("Services/MailInbox/FoodDiary.MailInbox.Infrastructure/Extensions/MailInboxServiceCollectionExtensions.cs");

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
        string mailRelay = ReadSource("Services/MailRelay/FoodDiary.MailRelay.Infrastructure/Extensions/MailRelayServiceCollectionExtensions.cs");
        string mailInbox = ReadSource("Services/MailInbox/FoodDiary.MailInbox.Infrastructure/Extensions/MailInboxServiceCollectionExtensions.cs");

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
    public void GrafanaReverseProxy_StreamsResponsesWithoutTemporaryFiles() {
        string nginx = ReadSource("nginx/sites-enabled/grafana.fooddiary.club");
        int proxyLocationStart = nginx.IndexOf("location / {", StringComparison.Ordinal);
        int liveLocationStart = nginx.IndexOf("location /api/live/ {", StringComparison.Ordinal);

        Assert.True(proxyLocationStart >= 0, "The Grafana proxy location is missing.");
        Assert.True(liveLocationStart > proxyLocationStart, "The Grafana live location is missing.");
        Assert.Contains("proxy_buffering off;", nginx[proxyLocationStart..liveLocationStart], StringComparison.Ordinal);
    }

    [Fact]
    public void PrimaryReverseProxy_RejectsUnknownHostsAndCanonicalizesForwardedHost() {
        string nginx = ReadSource("nginx/sites-enabled/fooddiary.club");

        Assert.Multiple(
            () => Assert.Contains("listen 80 default_server;", nginx, StringComparison.Ordinal),
            () => Assert.Contains("listen 443 ssl default_server;", nginx, StringComparison.Ordinal),
            () => Assert.Contains("listen 443 quic reuseport default_server;", nginx, StringComparison.Ordinal),
            () => Assert.Contains("ssl_reject_handshake on;", nginx, StringComparison.Ordinal),
            () => Assert.Equal(3, CountOccurrences(nginx, "proxy_set_header Host $server_name;")),
            () => Assert.Equal(3, CountOccurrences(nginx, "proxy_set_header X-Forwarded-Host $server_name;")),
            () => Assert.DoesNotContain("proxy_set_header Host $host;", nginx, StringComparison.Ordinal));
    }

    [Fact]
    public void ReverseProxy_AllowsOnlyModernTlsProtocols() {
        string[] configurationPaths = [
            ArchitectureTestPaths.FromRoot("nginx.conf"),
            .. Directory.GetFiles(ArchitectureTestPaths.FromRoot("nginx", "sites-enabled"), "*", SearchOption.TopDirectoryOnly),
        ];
        List<string> violations = [];

        foreach (string path in configurationPaths) {
            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++) {
                string line = lines[index].Trim();
                if (!line.StartsWith("ssl_protocols ", StringComparison.Ordinal) || line.StartsWith('#')) {
                    continue;
                }

                string[] protocols = line["ssl_protocols ".Length..]
                    .TrimEnd(';')
                    .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (!protocols.SequenceEqual(["TLSv1.2", "TLSv1.3"], StringComparer.Ordinal)) {
                    violations.Add($"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path)}:{(index + 1).ToString(CultureInfo.InvariantCulture)}: {line}");
                }
            }
        }

        Assert.Multiple(
            () => Assert.Contains("ssl_protocols TLSv1.2 TLSv1.3;", ReadSource("nginx.conf"), StringComparison.Ordinal),
            () => Assert.Empty(violations.Order(StringComparer.Ordinal)));
    }

    [Fact]
    public void BrowserApplications_EnforceContentSecurityPolicyAcrossStaticLocations() {
        string embeddedClient = ReadSource("FoodDiary.Web.Client/nginx.conf");
        string productionSite = ReadSource("nginx/sites-enabled/fooddiary.club");
        int adminServerStart = productionSite.IndexOf("server_name admin.fooddiary.club;", StringComparison.Ordinal);
        Assert.True(adminServerStart >= 0, "The production admin HTTPS server is missing.");
        string adminServer = productionSite[adminServerStart..];

        Assert.Multiple(
            () => Assert.Contains("add_header Content-Security-Policy ", embeddedClient, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Content-Security-Policy-Report-Only", embeddedClient, StringComparison.Ordinal),
            () => Assert.Contains("default-src 'self'", embeddedClient, StringComparison.Ordinal),
            () => Assert.Contains("object-src 'none'", embeddedClient, StringComparison.Ordinal),
            () => Assert.Contains("frame-ancestors https://web.telegram.org;", embeddedClient, StringComparison.Ordinal),
            () => Assert.Contains("https://cdn.paddle.com https://telegram.org;", embeddedClient, StringComparison.Ordinal),
            () => Assert.Contains("frame-ancestors https://web.telegram.org;", productionSite[..adminServerStart], StringComparison.Ordinal),
            () => Assert.Contains("https://cdn.paddle.com https://telegram.org;", productionSite[..adminServerStart], StringComparison.Ordinal),
            () => Assert.Contains("frame-ancestors 'none'", adminServer, StringComparison.Ordinal),
            () => Assert.Contains("add_header_inherit merge;", embeddedClient, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Content-Security-Policy-Report-Only", productionSite, StringComparison.Ordinal),
            () => Assert.Equal(2, CountOccurrences(productionSite, "add_header Content-Security-Policy ")),
            () => Assert.Contains("add_header Content-Security-Policy ", adminServer, StringComparison.Ordinal),
            () => Assert.Contains("add_header_inherit merge;", adminServer, StringComparison.Ordinal));
    }

    [Fact]
    public void ApiExceptionLogs_UseBoundedRouteLabels() {
        string exceptionHandler = ReadSource("FoodDiary.Web.Api/Extensions/ApiExceptionHandler.cs");

        Assert.Multiple(
            () => Assert.Contains("TelemetryPrivacyProcessor.ResolveRouteLabel(httpContext)", exceptionHandler, StringComparison.Ordinal),
            () => Assert.DoesNotContain("httpContext.Request.Path);", exceptionHandler, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Client/nginx.conf", 0, "FoodDiary.Web.Client/src/index.html")]
    [InlineData("nginx/sites-enabled/fooddiary.club", 0, "FoodDiary.Web.Client/src/index.html")]
    [InlineData("FoodDiary.Web.Client/nginx.telegram-test.conf", 0, "FoodDiary.Web.Client/src/index.html")]
    [InlineData("nginx/sites-enabled/fooddiary.club", 1, "FoodDiary.Web.Client/projects/fooddiary-admin/src/index.html")]
    public void BrowserContentSecurityPolicy_AllowsExistingFontStylesAndFiles(string configPath, int policyIndex, string htmlPath) {
        string policy = ReadContentSecurityPolicy(configPath, policyIndex);
        string[] styles = ReadPolicySources(policy, "style-src");
        string[] fonts = ReadPolicySources(policy, "font-src");
        MatchCollection links = Regex.Matches(ReadSource(htmlPath), "<link\\b[^>]*\\bhref=\"(?<url>https://fonts\\.googleapis\\.com/[^\"]+)\"",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        Assert.NotEmpty(links);

        foreach (Match link in links) {
            string origin = new Uri(link.Groups["url"].Value).GetLeftPart(UriPartial.Authority);
            Assert.Contains(origin, styles, StringComparer.Ordinal);
        }

        Assert.Contains("https://fonts.gstatic.com", fonts, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Client/nginx.conf", 0)]
    [InlineData("nginx/sites-enabled/fooddiary.club", 0)]
    [InlineData("nginx/sites-enabled/fooddiary.club", 1)]
    public void ProductionContentSecurityPolicy_AllowsCloudflareBeaconWithoutBroadScriptSources(string configPath, int policyIndex) {
        string[] scripts = ReadPolicySources(ReadContentSecurityPolicy(configPath, policyIndex), "script-src");

        Assert.Multiple(
            () => Assert.Contains("https://static.cloudflareinsights.com", scripts, StringComparer.Ordinal),
            () => Assert.DoesNotContain("*", scripts, StringComparer.Ordinal),
            () => Assert.DoesNotContain("https:", scripts, StringComparer.Ordinal),
            () => Assert.DoesNotContain("'unsafe-eval'", scripts, StringComparer.Ordinal));
    }

    [Fact]
    public void ClientContentSecurityPolicies_StayConsistentAcrossProductionLayers() {
        Assert.Equal(
            ReadContentSecurityPolicy("FoodDiary.Web.Client/nginx.conf", 0),
            ReadContentSecurityPolicy("nginx/sites-enabled/fooddiary.club", 0));
        Assert.DoesNotContain("https://static.cloudflareinsights.com",
            ReadPolicySources(ReadContentSecurityPolicy("FoodDiary.Web.Client/nginx.telegram-test.conf", 0), "script-src"), StringComparer.Ordinal);
    }

    private static string ReadContentSecurityPolicy(string configPath, int policyIndex) {
        MatchCollection policies = Regex.Matches(ReadSource(configPath), "add_header Content-Security-Policy \"(?<policy>[^\"]+)\" always;",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        Assert.True(policies.Count > policyIndex, $"{configPath}: CSP {policyIndex.ToString(CultureInfo.InvariantCulture)} is missing.");
        return policies[policyIndex].Groups["policy"].Value;
    }

    private static string[] ReadPolicySources(string policy, string directive) {
        string entry = Assert.Single(policy.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            value => value.StartsWith(directive + " ", StringComparison.Ordinal));
        return entry[(directive.Length + 1)..].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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
