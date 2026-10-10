using FoodDiary.Web.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ApiTaskConfigurationTests {
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
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
