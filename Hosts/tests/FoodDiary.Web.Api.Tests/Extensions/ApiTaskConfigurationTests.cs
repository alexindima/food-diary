using System.Net;
using FoodDiary.Web.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ApiTaskConfigurationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutboundGuardRegistration_DisablesRedirectsAfterTheInnerFilterAndRunsBeforeOtherHandlers(bool useSockets) {
        var services = new ServiceCollection();
        services.AddHttpClient();
        Assert.Same(services, services.AddTaskOutboundGuard());
        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpMessageHandlerBuilderFilter filter = provider.GetRequiredService<IHttpMessageHandlerBuilderFilter>();
        HttpMessageHandlerBuilder builder = provider.GetRequiredService<HttpMessageHandlerBuilder>();
        using HttpMessageHandler transport = useSockets ? new SocketsHttpHandler() : new HttpClientHandler();
        using var existing = new TaskOutboundHttpHandler();
        bool innerCalled = false;

        filter.Configure(next => {
            innerCalled = true;
            next.PrimaryHandler = transport;
            next.AdditionalHandlers.Add(existing);
        })(builder);

        Assert.Multiple(
            () => Assert.True(innerCalled),
            () => Assert.Same(transport, builder.PrimaryHandler),
            () => Assert.IsType<TaskOutboundHttpHandler>(builder.AdditionalHandlers[0]),
            () => Assert.Same(existing, builder.AdditionalHandlers[1]));
        if (transport is SocketsHttpHandler sockets) {
            Assert.False(sockets.AllowAutoRedirect);
        } else {
            Assert.False(((HttpClientHandler)transport).AllowAutoRedirect);
        }
        builder.AdditionalHandlers[0].Dispose();
    }

    [Fact]
    public void OutboundGuardRegistration_RejectsATransportThatCannotDisableRedirects() {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddTaskOutboundGuard();
        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpMessageHandlerBuilderFilter filter = provider.GetRequiredService<IHttpMessageHandlerBuilderFilter>();
        HttpMessageHandlerBuilder builder = provider.GetRequiredService<HttpMessageHandlerBuilder>();
        using var transport = new RecordingHandler();

        Assert.Throws<InvalidOperationException>(() => filter.Configure(next => next.PrimaryHandler = transport)(builder));
        Assert.Empty(builder.AdditionalHandlers);
    }

    [Theory]
    [InlineData("relative-task.json")]
    [InlineData("missing-absolute")]
    public void TaskConfiguration_RejectsMissingOrRelativePathsWithoutRemovingExistingProviders(string path) {
        if (string.Equals(path, "missing-absolute", StringComparison.Ordinal)) {
            path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "task.json");
        }
        IConfigurationBuilder configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["Marker"] = "original",
        });
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);

        Assert.Throws<InvalidOperationException>(() => configuration.AddTaskConfiguration(environment, path));
        Assert.Equal("original", configuration.Build()["Marker"]);
    }

    [Theory]
    [InlineData("https://provider.invalid/request")]
    [InlineData("http://localhost.provider.invalid/request")]
    [InlineData("file:///tmp/request")]
    public async Task OutboundGuard_RejectsExternalTransportBeforeSendingAsync(string target) {
        using var terminal = new RecordingHandler();
        using var handler = new TaskOutboundHttpHandler { InnerHandler = terminal };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(target));

        await Assert.ThrowsAsync<HttpRequestException>(() => invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(0, terminal.RequestCount);
    }

    [Theory]
    [InlineData("http://127.0.0.1:12345/health")]
    [InlineData("http://[::1]:12345/health")]
    public async Task OutboundGuard_AllowsOwnedLoopbackTransportAsync(string target) {
        using var terminal = new RecordingHandler();
        using var handler = new TaskOutboundHttpHandler { InnerHandler = terminal };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(target));

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(1, terminal.RequestCount);
    }
    [Fact]
    public void WithoutTaskConfiguration_PreservesNormalProviders() {
        IConfigurationBuilder configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["Marker"] = "normal",
        });
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);

        configuration.AddTaskConfiguration(environment, configurationPath: null);

        Assert.Equal("normal", configuration.Build()["Marker"]);
    }

    [Fact]
    public void TaskConfiguration_RejectsNonDevelopmentBeforeReadingFile() {
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);

        Assert.Throws<InvalidOperationException>(() =>
            new ConfigurationBuilder().AddTaskConfiguration(environment, Path.GetFullPath("missing-task-config.json")));
    }

    [Fact]
    public void TaskConfiguration_DoesNotInheritCredentialsOrEnvironmentProviders() {
        string file = Path.GetTempFileName();
        try {
            File.WriteAllText(file, """{"Marker":"owned","OpenAi":{"ApiKey":""}}""");
            IConfigurationBuilder configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["Marker"] = "inherited",
                ["OpenAi:ApiKey"] = "synthetic-inherited-key",
                ["Stripe:SecretKey"] = "synthetic-inherited-key",
            });
            IHostEnvironment environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(Environments.Development);

            IConfiguration result = configuration.AddTaskConfiguration(environment, file).Build();

            Assert.Multiple(() => {
                Assert.Equal("owned", result["Marker"]);
                Assert.Equal(string.Empty, result["OpenAi:ApiKey"]);
                Assert.Null(result["Stripe:SecretKey"]);
            });
        } finally {
            File.Delete(file);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler : HttpMessageHandler {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
