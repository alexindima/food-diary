using System.Net;
using System.Globalization;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramIntakeDeliveryTests {
    [Theory]
    [InlineData("Authentication.TelegramNotLinked", false)]
    [InlineData("Authentication.InvalidToken", false)]
    [InlineData("Authentication.AccountDeleted", false)]
    [InlineData("Authentication.TelegramNotLinked", true)]
    public async Task RejectedRegistration_BlockedReplyDoesNotBlockNextUser(string error, bool photo) {
        using var scenario = new IntakeScenario { RegistrationError = error, TelegramErrorCode = 403 };
        Update rejected = MessageUpdate(10, 100, "/today");
        if (photo) {
            rejected.Message!.Photo = [new PhotoSize { FileId = "food", FileUniqueId = "photo", Width = 400, Height = 400, FileSize = 1024 }];
        }

        await scenario.Receiver.ProcessBatchAsync([rejected, MessageUpdate(11, 200, "/today")], CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(12, scenario.Receiver.NextOffset),
            () => Assert.Equal(new long[] { 100, 200 }, scenario.RegistrationAttempts),
            () => Assert.Equal(new long[] { 200 }, scenario.PersistedUsers),
            () => Assert.Equal(new[] { "sendMessage" }, scenario.TelegramMethods, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("/help")]
    [InlineData("/water")]
    [InlineData("/start")]
    [InlineData("/settings")]
    [InlineData("menu:help")]
    [InlineData("menu:water")]
    [InlineData("menu:food")]
    public async Task FallbackReply_BlockedRecipientDoesNotBlockNextUser(string command) {
        using var scenario = new IntakeScenario { TelegramErrorCode = 403 };
        Update first = command.StartsWith("menu:", StringComparison.Ordinal)
            ? CallbackUpdate(10, command)
            : MessageUpdate(10, 100, command);

        await scenario.Receiver.ProcessBatchAsync([first, MessageUpdate(11, 200, "/today")], CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(12, scenario.Receiver.NextOffset),
            () => Assert.Equal(new long[] { 200 }, scenario.PersistedUsers),
            () => Assert.Equal(new[] { "sendMessage" }, scenario.TelegramMethods, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("menu:help", 400)]
    [InlineData("menu:water", 400)]
    [InlineData("water:invalid", 400)]
    [InlineData("stats:today", 400)]
    [InlineData("water:invalid", 403)]
    [InlineData("stats:today", 403)]
    public async Task TerminalCallbackRejection_DoesNotBlockNextUser(string callback, int errorCode) {
        using var scenario = new IntakeScenario {
            TelegramErrorCode = errorCode,
            ErrorMethod = "answerCallbackQuery",
        };

        await scenario.Receiver.ProcessBatchAsync([CallbackUpdate(10, callback), MessageUpdate(11, 200, "/today")], CancellationToken.None);

        long[] expectedUsers = string.Equals(callback, "stats:today", StringComparison.Ordinal) ? [100, 200] : [200];
        Assert.Multiple(
            () => Assert.Equal(12, scenario.Receiver.NextOffset),
            () => Assert.Equal(expectedUsers, scenario.PersistedUsers),
            () => Assert.Contains("answerCallbackQuery", scenario.TelegramMethods, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(429)]
    [InlineData(503)]
    public async Task OtherTelegramErrors_RetainFailedUpdateForRetry(int errorCode) {
        using var scenario = new IntakeScenario { TelegramErrorCode = errorCode };
        Update[] batch = [MessageUpdate(10, 200, "/today"), MessageUpdate(11, 100, "/help"), MessageUpdate(12, 300, "/today")];

        ApiRequestException failure = await Assert.ThrowsAsync<ApiRequestException>(() => scenario.Receiver.ProcessBatchAsync(batch, CancellationToken.None));
        Assert.Multiple(
            () => Assert.Equal(errorCode, failure.ErrorCode),
            () => Assert.Equal(11, scenario.Receiver.NextOffset),
            () => Assert.Equal(new long[] { 200 }, scenario.PersistedUsers));
        scenario.TelegramErrorCode = null;

        await scenario.Receiver.ProcessBatchAsync(batch, CancellationToken.None);

        Assert.Equal(13, scenario.Receiver.NextOffset);
        Assert.Equal(new long[] { 200, 300 }, scenario.PersistedUsers);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task RegistrationFailure_RetainsFailedUpdateForRetry(HttpStatusCode status) {
        using var scenario = new IntakeScenario { RegistrationError = "Persistence.Unavailable", RegistrationStatus = status };
        Update[] batch = [MessageUpdate(10, 200, "/today"), MessageUpdate(11, 100, "/today"), MessageUpdate(12, 300, "/today")];

        BotOperationApiException failure = await Assert.ThrowsAsync<BotOperationApiException>(() => scenario.Receiver.ProcessBatchAsync(batch, CancellationToken.None));
        Assert.Multiple(
            () => Assert.Equal(status, failure.StatusCode),
            () => Assert.Equal(11, scenario.Receiver.NextOffset),
            () => Assert.Equal(new long[] { 200 }, scenario.PersistedUsers),
            () => Assert.Empty(scenario.TelegramMethods));
        scenario.RegistrationError = null;

        await scenario.Receiver.ProcessBatchAsync(batch, CancellationToken.None);

        Assert.Equal(13, scenario.Receiver.NextOffset);
        Assert.Equal(new long[] { 200, 100, 300 }, scenario.PersistedUsers);
        Assert.Equal(new long[] { 200, 100, 100, 300 }, scenario.RegistrationAttempts);
    }

    [Fact]
    public async Task TelegramTransportFailure_RemainsUnacknowledged() {
        using var scenario = new IntakeScenario { TelegramFailure = new RequestException("Transport unavailable") };

        await Assert.ThrowsAsync<RequestException>(() => scenario.Receiver.ProcessBatchAsync(
            [MessageUpdate(10, 100, "/help"), MessageUpdate(11, 200, "/today")], CancellationToken.None));

        Assert.Null(scenario.Receiver.NextOffset);
        Assert.Empty(scenario.PersistedUsers);
    }

    [Fact]
    public async Task CancellationDuringRegistration_RemainsUnacknowledged() {
        using var stopping = new CancellationTokenSource();
        using var scenario = new IntakeScenario { CancelRegistration = stopping };
        Update[] batch = [MessageUpdate(10, 200, "/today"), MessageUpdate(11, 100, "/today"), MessageUpdate(12, 300, "/today")];

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scenario.Receiver.ProcessBatchAsync(batch, stopping.Token));

        Assert.Equal(11, scenario.Receiver.NextOffset);
        Assert.Equal(new long[] { 200 }, scenario.PersistedUsers);
        Assert.Empty(scenario.TelegramMethods);
    }

    [Fact]
    public async Task SuccessfulCallback_RegistersBeforeAcknowledgementAndContinues() {
        using var scenario = new IntakeScenario();

        await scenario.Receiver.ProcessBatchAsync([CallbackUpdate(10, "stats:today"), MessageUpdate(11, 200, "/today")], CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(12, scenario.Receiver.NextOffset),
            () => Assert.Equal(new long[] { 100, 200 }, scenario.PersistedUsers),
            () => Assert.Equal(new[] { "register:100", "answerCallbackQuery", "register:200" }, scenario.Events, StringComparer.Ordinal));
    }

    private static Update MessageUpdate(int id, long userId, string text) => new() {
        Id = id,
        Message = new Message {
            Id = id,
            Date = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc),
            Text = text,
            From = new User { Id = userId, FirstName = "Test" },
            Chat = new Chat { Id = userId, Type = ChatType.Private },
        },
    };

    private static Update CallbackUpdate(int id, string data) => new() {
        Id = id,
        CallbackQuery = new CallbackQuery {
            Id = string.Create(CultureInfo.InvariantCulture, $"callback-{id}"),
            Data = data,
            From = new User { Id = 100, FirstName = "Test" },
            Message = new Message { Id = id, Chat = new Chat { Id = 100, Type = ChatType.Private } },
        },
    };

    [ExcludeFromCodeCoverage]
    private sealed class IntakeScenario : IDisposable {
        private readonly Handler _apiHandler;
        private readonly Handler _telegramHandler;
        private readonly HttpClient _telegramHttp;
        private readonly TelegramBotWorker _worker;

        public string? RegistrationError { get; set; }
        public HttpStatusCode RegistrationStatus { get; init; } = HttpStatusCode.Unauthorized;
        public int? TelegramErrorCode { get; set; }
        public string ErrorMethod { get; init; } = "sendMessage";
        public Exception? TelegramFailure { get; init; }
        public CancellationTokenSource? CancelRegistration { get; init; }
        public List<long> RegistrationAttempts { get; } = [];
        public List<long> PersistedUsers { get; } = [];
        public List<string> TelegramMethods { get; } = [];
        public List<string> Events { get; } = [];
        public DurableTelegramReceiver Receiver { get; }

        public IntakeScenario() {
            _apiHandler = new Handler(RespondApiAsync);
            _telegramHandler = new Handler(RespondTelegramAsync);
            _telegramHttp = new HttpClient(_telegramHandler, disposeHandler: false);
            var bot = new TelegramBotClient(new TelegramBotClientOptions("123:test") { RetryCount = 0 }, _telegramHttp);
            _worker = new TelegramBotWorker(bot, Options.Create(new TelegramBotOptions {
                Token = "123:test",
                OperationsEnabled = true,
                ApiBaseUrl = "https://api.example.test",
                ApiSecret = "test-operation-secret",
                WebAppUrl = "https://app.example.test",
            }), NullLogger<TelegramBotWorker>.Instance, new Factory(_apiHandler), TimeProvider.System);
            MethodInfo method = typeof(TelegramBotWorker).GetMethod("PersistOrHandleUpdateAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Func<Update, CancellationToken, Task> process = method.CreateDelegate<Func<Update, CancellationToken, Task>>(_worker);
            Receiver = new DurableTelegramReceiver(bot, process, TimeProvider.System, NullLogger.Instance);
        }

        private async Task<HttpResponseMessage> RespondApiAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (request.RequestUri!.AbsolutePath.EndsWith("/bot/auth", StringComparison.Ordinal)) {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
            using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            long userId = body.RootElement.GetProperty("telegramUserId").GetInt64();
            RegistrationAttempts.Add(userId);
            if (userId == 100) {
                if (CancelRegistration is not null) {
                    await CancelRegistration.CancelAsync();
                    cancellationToken.ThrowIfCancellationRequested();
                }
                if (RegistrationError is not null) {
                    return new HttpResponseMessage(RegistrationStatus) { Content = JsonContent.Create(new { error = RegistrationError }) };
                }
            }
            PersistedUsers.Add(userId);
            Events.Add(string.Create(CultureInfo.InvariantCulture, $"register:{userId}"));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { operationId = Guid.NewGuid() }) };
        }

        private Task<HttpResponseMessage> RespondTelegramAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            string method = request.RequestUri!.Segments[^1];
            TelegramMethods.Add(method);
            Events.Add(method);
            if (TelegramFailure is not null) {
                return Task.FromException<HttpResponseMessage>(TelegramFailure);
            }
            if (TelegramErrorCode is { } code && string.Equals(method, ErrorMethod, StringComparison.OrdinalIgnoreCase)) {
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)code) {
                    Content = JsonContent.Create(new { ok = false, error_code = code, description = "Delivery rejected" }),
                });
            }
            object response = string.Equals(method, "answerCallbackQuery", StringComparison.OrdinalIgnoreCase)
                ? new { ok = true, result = (object)true }
                : new { ok = true, result = (object)new { message_id = 20, date = 1792238400, chat = new { id = 100, type = "private" }, text = "Reply" } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });
        }

        public void Dispose() {
            _worker.Dispose();
            _telegramHttp.Dispose();
            _apiHandler.Dispose();
            _telegramHandler.Dispose();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken);
    }
}
