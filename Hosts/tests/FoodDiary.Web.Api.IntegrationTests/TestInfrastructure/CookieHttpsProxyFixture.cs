using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Testcontainers.Redis;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class CookieHttpsProxyFixture : IAsyncLifetime {
    private IContainer? _proxy;
    private X509Certificate2? _certificate;
    private RedisContainer? _redis;
    public PostgresApiWebApplicationFactory Api { get; } = new HttpsProxyApiFactory();
    public Uri Origin { get; private set; } = null!;

    public async Task InitializeAsync() {
        if (!DockerAvailability.IsAvailable(out _)) {
            return;
        }
        using var readiness = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        _redis = new RedisBuilder("redis:7-alpine")
            .WithCreateParameterModifier(parameters => {
                parameters.HostConfig ??= new HostConfig();
                parameters.HostConfig.PortBindings ??= new Dictionary<string, IList<PortBinding>>(StringComparer.Ordinal);
                parameters.HostConfig.PortBindings["6379/tcp"] = [new PortBinding { HostIP = IPAddress.Loopback.ToString(), HostPort = "0" }];
            }).Build();
        await _redis.StartAsync(readiness.Token);
        ((HttpsProxyApiFactory)Api).RedisConnectionString = _redis.GetConnectionString();
        await Api.InitializeAsync();
        Api.UseKestrel(0);
        using HttpClient upstream = Api.CreateClient();
        int port = upstream.BaseAddress!.Port;
        // Testcontainers forwards only this ephemeral loopback Kestrel port into its private SSH tunnel.
        await TestcontainersSettings.ExposeHostPortsAsync(checked((ushort)port), readiness.Token);
        await StartProxyAsync(port, readiness.Token);
        using HttpClient probe = CreateBrowser();
        using HttpResponseMessage response = await probe.GetAsync("/health/live", readiness.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("nginx", response.Headers.Server.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task StartProxyAsync(int port, CancellationToken cancellationToken) {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
        _certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
        string configuration = FormattableString.Invariant($$"""
            events {}
            http {
                access_log off;
                server {
                    listen 443 ssl;
                    server_name localhost;
                    ssl_certificate /test/cert.pem;
                    ssl_certificate_key /test/key.pem;
                    location / {
                        proxy_pass http://host.testcontainers.internal:{{port}};
                        proxy_set_header Host $http_host;
                        proxy_set_header X-Forwarded-Host $http_host;
                        proxy_set_header X-Forwarded-Proto $scheme;
                        proxy_set_header X-Forwarded-For $remote_addr;
                    }
                }
            }
            """);
        _proxy = new ContainerBuilder("nginx:alpine@sha256:892bb2152464f0c5d8972593a5f5afaf8054552580b699d95f8ff4af08c2d33d")
            .WithResourceMapping(Encoding.UTF8.GetBytes(configuration), "/etc/nginx/nginx.conf")
            .WithResourceMapping(Encoding.UTF8.GetBytes(_certificate.ExportCertificatePem()), "/test/cert.pem")
            .WithResourceMapping(Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem()), "/test/key.pem")
            .WithPortBinding(443, assignRandomHostPort: true)
            .WithCreateParameterModifier(parameters => {
                parameters.HostConfig ??= new HostConfig();
                parameters.HostConfig.PortBindings ??= new Dictionary<string, IList<PortBinding>>(StringComparer.Ordinal);
                parameters.HostConfig.PortBindings["443/tcp"] = [new PortBinding { HostIP = IPAddress.Loopback.ToString(), HostPort = "0" }];
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(443, options => options.WithTimeout(TimeSpan.FromSeconds(45))))
            .Build();
        await _proxy.StartAsync(cancellationToken);
        Origin = new Uri(FormattableString.Invariant($"https://localhost:{_proxy.GetMappedPublicPort(443)}"));
    }

    public HttpClient CreateBrowser(CookieContainer? cookies = null) {
        var handler = new SocketsHttpHandler {
            CookieContainer = cookies ?? new CookieContainer(),
            SslOptions = new SslClientAuthenticationOptions {
                CertificateChainPolicy = new X509ChainPolicy {
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    CustomTrustStore = { _certificate! },
                    RevocationMode = X509RevocationMode.NoCheck,
                },
            },
        };
        var browser = new HttpClient(handler) { BaseAddress = Origin };
        browser.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return browser;
    }

    public async Task DisposeAsync() {
        try {
            if (_proxy is not null) { await _proxy.DisposeAsync(); }
        } finally {
            _certificate?.Dispose();
            try {
                await Api.DisposeAsync();
            } finally {
                if (_redis is not null) { await _redis.DisposeAsync(); }
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class HttpsProxyApiFactory : PostgresApiWebApplicationFactory {
        public string RedisConnectionString { get; set; } = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("CookieHttpsTests");
            builder.UseSetting("ConnectionStrings:Redis", RedisConnectionString);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:Redis"] = RedisConnectionString,
                ["MailRelayClient:BaseUrl"] = "https://127.0.0.1:1",
                ["MailInboxClient:BaseUrl"] = "https://127.0.0.1:1",
                ["Cors:Origins:0"] = "https://trusted-other.example",
                ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
                ["ForwardedHeaders:KnownProxies:1"] = "::1",
                ["ForwardedHeaders:KnownProxies:2"] = "::ffff:127.0.0.1",
                ["ForwardedHeaders:AllowedHosts:0"] = "localhost",
            }));
        }
    }
}
