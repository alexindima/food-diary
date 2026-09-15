using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FoodDiary.Web.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class OpenTelemetryConfigurationTests {
    [Fact]
    public void AddApiServices_WithoutOtlpEndpoint_DoesNotRegisterTelemetryProviders() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(otlpEndpoint: null);

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<TracerProvider>());
        Assert.Null(provider.GetService<MeterProvider>());
    }

    [Fact]
    public void AddApiServices_WithValidOtlpEndpoint_RegistersTelemetryProviders() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration("http://localhost:4317");

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<TracerProvider>());
        Assert.NotNull(provider.GetService<MeterProvider>());
    }

    [Fact]
    public void AddApiServices_WithInvalidOtlpEndpoint_Throws() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration("not-a-uri");

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => services.AddApiServices(configuration));
        Assert.Contains("OpenTelemetry:Otlp:Endpoint", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddApiServices_WithValidOtlpEndpoint_PropagatesW3CTraceContext() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration("http://localhost:4317");

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<TracerProvider>());

        var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        try {
            IPEndPoint endpoint = Assert.IsType<IPEndPoint>(listener.LocalEndpoint);
            Task<string> traceParentTask = CaptureTraceParentAsync(listener);
            using var client = new HttpClient();
            using Activity? activity = ApiTelemetry.ActivitySource.StartActivity("w3c-propagation-test");
            Assert.NotNull(activity);

            string requestUri = string.Create(
                CultureInfo.InvariantCulture,
                $"http://127.0.0.1:{endpoint.Port}/resource?token=private");
            using HttpResponseMessage response = await client.GetAsync(
                new Uri(requestUri, UriKind.Absolute));
            string traceParent = await traceParentTask;

            Assert.Multiple(
                () => Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat),
                () => Assert.StartsWith("00-", traceParent, StringComparison.Ordinal),
                () => Assert.Contains(activity.TraceId.ToHexString(), traceParent, StringComparison.Ordinal));
        } finally {
            listener.Stop();
        }
    }

    private static IConfiguration CreateConfiguration(string? otlpEndpoint) {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
            ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
            ["Jwt:Issuer"] = "FoodDiaryApi",
            ["Jwt:Audience"] = "FoodDiaryClient",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["TelegramBot:ApiSecret"] = "",
            ["OpenTelemetry:Otlp:Endpoint"] = otlpEndpoint,
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static async Task<string> CaptureTraceParentAsync(TcpListener listener) {
        using TcpClient connection = await listener.AcceptTcpClientAsync();
        await using NetworkStream stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        string traceParent = string.Empty;
        while (await reader.ReadLineAsync() is { } line && line.Length > 0) {
            const string headerPrefix = "traceparent:";
            if (line.StartsWith(headerPrefix, StringComparison.OrdinalIgnoreCase)) {
                traceParent = line[headerPrefix.Length..].Trim();
            }
        }

        byte[] response = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray();
        await stream.WriteAsync(response);
        return traceParent;
    }
}
