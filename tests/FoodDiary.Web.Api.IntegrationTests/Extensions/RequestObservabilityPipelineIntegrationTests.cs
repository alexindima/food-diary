using System.Net;
using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class RequestObservabilityPipelineIntegrationTests(ApiWebApplicationFactory apiFactory)
    : IClassFixture<ApiWebApplicationFactory> {
    [Fact]
    public async Task Pipeline_WhenConcurrencyExceptionIsHandled_LogsFinalConflictStatus() {
        var loggerProvider = new RecordingLoggerProvider();
        await using WebApplicationFactory<Program> factory = apiFactory.WithWebHostBuilder(builder => {
            builder.ConfigureLogging(logging => {
                logging.ClearProviders();
                logging.AddProvider(loggerProvider);
            });
        });
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/test/exceptions/concurrency");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(
            loggerProvider.Messages,
            message => string.Equals(message.Category, typeof(RequestObservabilityMiddleware).FullName, StringComparison.Ordinal) &&
                       message.Text.Contains("HTTP GET /test/exceptions/concurrency responded 409", StringComparison.Ordinal));
        Assert.DoesNotContain(
            loggerProvider.Messages,
            message => string.Equals(message.Category, typeof(RequestObservabilityMiddleware).FullName, StringComparison.Ordinal) &&
                       message.Text.Contains("HTTP GET /test/exceptions/concurrency responded 200", StringComparison.Ordinal));
    }

    [ExcludeFromCodeCoverage]
    private sealed record LogMessage(string Category, string Text);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLoggerProvider : ILoggerProvider {
        private readonly Lock _gate = new();
        private readonly List<LogMessage> _messages = [];

        public IReadOnlyList<LogMessage> Messages {
            get {
                lock (_gate) {
                    return [.. _messages];
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, this);

        public void Dispose() {
        }

        private void Add(string categoryName, string message) {
            lock (_gate) {
                _messages.Add(new LogMessage(categoryName, message));
            }
        }

        [ExcludeFromCodeCoverage]
        private sealed class RecordingLogger(string categoryName, RecordingLoggerProvider provider) : ILogger {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull =>
                null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) {
                provider.Add(categoryName, formatter(state, exception));
            }
        }
    }
}
