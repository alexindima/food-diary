using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramBotWorkerTests {
    [Fact]
    public async Task StartAsync_WhenTokenIsMissing_DoesNotCallTelegramApi() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), new TelegramBotOptions {
            Token = ""
        });

        await worker.StartAsync(CancellationToken.None);

        Assert.Empty(botClient.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsConfiguredAndStoppingTokenIsCancelled_StopsAfterStartingReceiver() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await InvokeExecuteAsync(worker, cts.Token);

        Assert.Contains(botClient.Requests, request => string.Equals(request.GetType().Name, "GetMeRequest", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoppingTokenIsCancelledAfterReceiverStarts_CompletesGracefully() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await InvokeExecuteAsync(worker, cts.Token);

        Assert.Contains(botClient.Requests, request => string.Equals(request.GetType().Name, "GetMeRequest", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoppingTokenCancelsDelay_LogsStopping() {
        var botClient = new RecordingTelegramBotClient();
        var logger = new RecordingLogger<TelegramBotWorker>();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions(), logger);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await InvokeExecuteAsync(worker, cts.Token);

        Assert.Contains(logger.Messages, message => string.Equals(message, "Telegram bot stopping.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WhenBotUsernameIsMissing_UsesBotIdForStartupLog() {
        var botClient = new RecordingTelegramBotClient {
            MeResponse = new User {
                Id = 456,
                IsBot = true,
                FirstName = "FoodDiary"
            }
        };
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await InvokeExecuteAsync(worker, cts.Token);

        Assert.Contains(botClient.Requests, request => string.Equals(request.GetType().Name, "GetMeRequest", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_WhenStoppedAfterReceiverStarts_CompletesGracefully() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Contains(botClient.Requests, request => string.Equals(request.GetType().Name, "GetMeRequest", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleUpdateAsync_WithHelpCommand_SendsHelpMessage() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateMessageUpdate("/help", telegramUserId: 100));

        object request = Assert.Single(botClient.Requests);
        Assert.Equal("SendMessageRequest", request.GetType().Name);
        Assert.Contains("Available commands", GetPropertyValue<string>(request, "Text"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleUpdateAsync_WithUnknownCommand_SendsHelpMessage() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateMessageUpdate("/unknown", telegramUserId: 100));

        object request = Assert.Single(botClient.Requests);
        Assert.Equal("SendMessageRequest", request.GetType().Name);
        Assert.Contains("Available commands", GetPropertyValue<string>(request, "Text"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleUpdateAsync_WithUnsupportedUpdate_IgnoresIt() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, new Update {
            Id = 1,
            EditedMessage = new Message {
                Id = 11,
                Date = DateTime.UtcNow,
                Chat = new Chat {
                    Id = 123,
                    Type = ChatType.Private
                }
            }
        });

        Assert.Empty(botClient.Requests);
    }

    [Fact]
    public async Task HandleUpdateAsync_WithMessageWithoutText_IgnoresIt() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, new Update {
            Id = 1,
            Message = new Message {
                Id = 10,
                Date = DateTime.UtcNow,
                Chat = new Chat {
                    Id = 123,
                    Type = ChatType.Private
                }
            }
        });

        Assert.Empty(botClient.Requests);
    }

    [Fact]
    public async Task HandleUpdateAsync_StartWhenUserIsNotLinked_SendsWebAppMessage() {
        var botClient = new RecordingTelegramBotClient();
        var httpFactory = new RecordingHttpClientFactory(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        TelegramBotWorker worker = CreateWorker(botClient, httpFactory, CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateMessageUpdate("/start", telegramUserId: 100));

        object request = Assert.Single(botClient.Requests);
        Assert.Equal("SendMessageRequest", request.GetType().Name);
        Assert.Contains("open the WebApp", GetPropertyValue<string>(request, "Text"), StringComparison.Ordinal);
        Assert.NotNull(GetPropertyValue<object?>(request, "ReplyMarkup"));
        Assert.Equal("https://api.example.test/api/auth/telegram/bot/auth", httpFactory.Requests.Single().RequestUri?.ToString());
    }

    [Fact]
    public async Task HandleUpdateAsync_StartWhenUserIsNotLinkedAndWebAppUrlIsMissing_SendsMessageWithoutMarkup() {
        var botClient = new RecordingTelegramBotClient();
        var httpFactory = new RecordingHttpClientFactory(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        TelegramBotWorker worker = CreateWorker(botClient, httpFactory, new TelegramBotOptions {
            Token = "telegram-token",
            WebAppUrl = "",
            ApiBaseUrl = "https://api.example.test/",
            ApiSecret = "telegram-api-secret-123"
        });

        await InvokeHandleUpdateAsync(worker, botClient, CreateMessageUpdate("/start", telegramUserId: 100));

        object request = Assert.Single(botClient.Requests);
        Assert.Contains("open the WebApp", GetPropertyValue<string>(request, "Text"), StringComparison.Ordinal);
        Assert.Null(GetPropertyValue<object?>(request, "ReplyMarkup"));
    }

    [Fact]
    public async Task HandleUpdateAsync_StartWhenTelegramUserIsMissing_DoesNotCallBackend() {
        var botClient = new RecordingTelegramBotClient();
        var httpFactory = new RecordingHttpClientFactory();
        TelegramBotWorker worker = CreateWorker(botClient, httpFactory, CreateOptions());
        Update update = CreateMessageUpdate("/start", telegramUserId: 100);
        update.Message!.From = null;

        await InvokeHandleUpdateAsync(worker, botClient, update);

        object request = Assert.Single(botClient.Requests);
        Assert.Contains("open the WebApp", GetPropertyValue<string>(request, "Text"), StringComparison.Ordinal);
        Assert.Empty(httpFactory.Requests);
    }

    [Fact]
    public async Task HandleUpdateAsync_StartWhenUserIsLinked_SendsQuickActions() {
        var botClient = new RecordingTelegramBotClient();
        var httpFactory = new RecordingHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(new {
                accessToken = "access-token",
                refreshToken = "refresh-token"
            })
        });
        TelegramBotWorker worker = CreateWorker(botClient, httpFactory, CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateMessageUpdate("/start", telegramUserId: 100));

        object request = Assert.Single(botClient.Requests);
        Assert.Equal("Quick actions:", GetPropertyValue<string>(request, "Text"));
        Assert.NotNull(GetPropertyValue<object?>(request, "ReplyMarkup"));
    }

    [Fact]
    public async Task HandleUpdateAsync_CallbackWithInvalidAmount_AnswersCallback() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateCallbackUpdate("water:bad"));

        object request = Assert.Single(botClient.Requests);
        Assert.Equal("AnswerCallbackQueryRequest", request.GetType().Name);
        Assert.Equal("Invalid amount.", GetPropertyValue<string>(request, "Text"));
    }

    [Fact]
    public async Task HandleUpdateAsync_CallbackWithoutData_IgnoresIt() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());
        Update update = CreateCallbackUpdate("water:250");
        update.CallbackQuery!.Data = null;

        await InvokeHandleUpdateAsync(worker, botClient, update);

        Assert.Empty(botClient.Requests);
    }

    [Fact]
    public async Task HandleUpdateAsync_CallbackWithoutUser_IgnoresIt() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());
        Update update = CreateCallbackUpdate("water:250");
        update.CallbackQuery!.From = null!;

        await InvokeHandleUpdateAsync(worker, botClient, update);

        Assert.Empty(botClient.Requests);
    }

    [Fact]
    public async Task HandleUpdateAsync_CallbackWithNonWaterData_IgnoresIt() {
        var botClient = new RecordingTelegramBotClient();
        TelegramBotWorker worker = CreateWorker(botClient, new RecordingHttpClientFactory(), CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateCallbackUpdate("meal:add"));

        Assert.Empty(botClient.Requests);
    }

    [Fact]
    public async Task HandleUpdateAsync_CallbackWhenUserIsNotLinked_AnswersCallback() {
        var botClient = new RecordingTelegramBotClient();
        var httpFactory = new RecordingHttpClientFactory(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        TelegramBotWorker worker = CreateWorker(botClient, httpFactory, CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateCallbackUpdate("water:250"));

        object request = Assert.Single(botClient.Requests);
        Assert.Equal("Please open the WebApp and log in once.", GetPropertyValue<string>(request, "Text"));
    }

    [Fact]
    public async Task HandleUpdateAsync_CallbackWhenHydrationSucceeds_AnswersAdded() {
        var botClient = new RecordingTelegramBotClient();
        var httpFactory = new RecordingHttpClientFactory(
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent.Create(new {
                    accessToken = "access-token",
                    refreshToken = "refresh-token"
                })
            },
            new HttpResponseMessage(HttpStatusCode.Created));
        TelegramBotWorker worker = CreateWorker(botClient, httpFactory, CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateCallbackUpdate("water:500"));

        object request = Assert.Single(botClient.Requests);
        Assert.Equal("Added 500 ml.", GetPropertyValue<string>(request, "Text"));
        Assert.Equal("Bearer", httpFactory.Requests[1].Headers.Authorization?.Scheme);
        Assert.Equal("access-token", httpFactory.Requests[1].Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task HandleUpdateAsync_CallbackWhenHydrationFails_AnswersFailure() {
        var botClient = new RecordingTelegramBotClient();
        var httpFactory = new RecordingHttpClientFactory(
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent.Create(new {
                    accessToken = "access-token",
                    refreshToken = "refresh-token"
                })
            },
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        TelegramBotWorker worker = CreateWorker(botClient, httpFactory, CreateOptions());

        await InvokeHandleUpdateAsync(worker, botClient, CreateCallbackUpdate("water:250"));

        object request = Assert.Single(botClient.Requests);
        Assert.Equal("Failed to add water.", GetPropertyValue<string>(request, "Text"));
    }

    [Fact]
    public async Task TryGetAccessTokenAsync_WhenApiSettingsAreInvalid_ReturnsNullAndDoesNotCallBackend() {
        var httpFactory = new RecordingHttpClientFactory();
        TelegramBotWorker worker = CreateWorker(new RecordingTelegramBotClient(), httpFactory, new TelegramBotOptions {
            Token = "telegram-token",
            ApiBaseUrl = "not-a-url",
            ApiSecret = ""
        });

        string? token = await InvokeTryGetAccessTokenAsync(worker, telegramUserId: 100);

        Assert.Null(token);
        Assert.Empty(httpFactory.Requests);
    }

    [Fact]
    public async Task TryGetAccessTokenAsync_WhenResponsePayloadIsNull_ReturnsNull() {
        var httpFactory = new RecordingHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("null")
        });
        TelegramBotWorker worker = CreateWorker(new RecordingTelegramBotClient(), httpFactory, CreateOptions());

        string? token = await InvokeTryGetAccessTokenAsync(worker, telegramUserId: 100);

        Assert.Null(token);
    }

    [Fact]
    public async Task CreateHydrationAsync_WhenApiBaseUrlIsInvalid_ReturnsFalseAndDoesNotCallBackend() {
        var httpFactory = new RecordingHttpClientFactory();
        TelegramBotWorker worker = CreateWorker(new RecordingTelegramBotClient(), httpFactory, new TelegramBotOptions {
            Token = "telegram-token",
            ApiBaseUrl = "not-a-url",
            ApiSecret = "telegram-api-secret-123"
        });

        bool success = await InvokeCreateHydrationAsync(worker, accessToken: "access-token", amountMl: 250);

        Assert.False(success);
        Assert.Empty(httpFactory.Requests);
    }

    [Fact]
    public async Task HandleErrorAsync_WithApiRequestException_Completes() {
        TelegramBotWorker worker = CreateWorker(new RecordingTelegramBotClient(), new RecordingHttpClientFactory(), CreateOptions());

        await InvokeHandleErrorAsync(worker, new ApiRequestException("bad request", 400));
    }

    [Fact]
    public async Task HandleErrorAsync_WithUnexpectedException_Completes() {
        TelegramBotWorker worker = CreateWorker(new RecordingTelegramBotClient(), new RecordingHttpClientFactory(), CreateOptions());

        await InvokeHandleErrorAsync(worker, new InvalidOperationException("boom"));
    }

    private static TelegramBotOptions CreateOptions() =>
        new() {
            Token = "telegram-token",
            WebAppUrl = "https://app.example.test/",
            ApiBaseUrl = "https://api.example.test/",
            ApiSecret = "telegram-api-secret-123"
        };

    private static TelegramBotWorker CreateWorker(
        ITelegramBotClient botClient,
        IHttpClientFactory httpClientFactory,
        TelegramBotOptions options,
        ILogger<TelegramBotWorker>? logger = null) =>
        new(
            botClient,
            Options.Create(options),
            logger ?? NullLogger<TelegramBotWorker>.Instance,
            httpClientFactory);

    private static Update CreateMessageUpdate(string text, long telegramUserId) =>
        new() {
            Id = 1,
            Message = new Message {
                Id = 10,
                Text = text,
                Date = DateTime.UtcNow,
                From = new User {
                    Id = telegramUserId,
                    FirstName = "Alex"
                },
                Chat = new Chat {
                    Id = 123,
                    Type = ChatType.Private
                }
            }
        };

    private static Update CreateCallbackUpdate(string data) =>
        new() {
            Id = 1,
            CallbackQuery = new CallbackQuery {
                Id = "callback-id",
                Data = data,
                From = new User {
                    Id = 100,
                    FirstName = "Alex"
                }
            }
        };

    private static Task InvokeHandleUpdateAsync(
        TelegramBotWorker worker,
        ITelegramBotClient botClient,
        Update update) {
        MethodInfo? method = typeof(TelegramBotWorker).GetMethod(
            "HandleUpdateAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (Task)method!.Invoke(worker, [botClient, update, CancellationToken.None])!;
    }

    private static Task InvokeExecuteAsync(TelegramBotWorker worker, CancellationToken cancellationToken) {
        MethodInfo? method = typeof(TelegramBotWorker).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (Task)method!.Invoke(worker, [cancellationToken])!;
    }

    private static Task InvokeHandleErrorAsync(TelegramBotWorker worker, Exception exception) {
        MethodInfo? method = typeof(TelegramBotWorker).GetMethod(
            "HandleErrorAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (Task)method!.Invoke(worker, [new RecordingTelegramBotClient(), exception, CancellationToken.None])!;
    }

    private static async Task<string?> InvokeTryGetAccessTokenAsync(TelegramBotWorker worker, long telegramUserId) {
        MethodInfo? method = typeof(TelegramBotWorker).GetMethod(
            "TryGetAccessTokenAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var task = (Task<string?>)method!.Invoke(worker, [telegramUserId, CancellationToken.None])!;
        return await task.ConfigureAwait(false);
    }

    private static async Task<bool> InvokeCreateHydrationAsync(
        TelegramBotWorker worker,
        string accessToken,
        int amountMl) {
        MethodInfo? method = typeof(TelegramBotWorker).GetMethod(
            "CreateHydrationAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var task = (Task<bool>)method!.Invoke(worker, [accessToken, amountMl, CancellationToken.None])!;
        return await task.ConfigureAwait(false);
    }

    private static TValue GetPropertyValue<TValue>(object instance, string propertyName) =>
        (TValue)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger<T> : ILogger<T> {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Messages.Add(formatter(state, exception));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingTelegramBotClient : ITelegramBotClient {
        public List<object> Requests { get; } = [];

        public User MeResponse { get; init; } = new() {
            Id = 123,
            IsBot = true,
            FirstName = "FoodDiary",
            Username = "fooddiary_bot"
        };

        public bool LocalBotServer => false;

        public long BotId => 123;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public IExceptionParser ExceptionsParser { get; set; } = new DefaultExceptionParser();

        public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest {
            add { }
            remove { }
        }

        public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived {
            add { }
            remove { }
        }

        public Task<TResponse> SendRequest<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default) {
            Requests.Add(request);

            object response = request.GetType().Name switch {
                "GetMeRequest" => MeResponse,
                "SendMessageRequest" => new Message {
                    Id = 20,
                    Date = DateTime.UtcNow,
                    Chat = new Chat {
                        Id = 123,
                        Type = ChatType.Private
                    }
                },
                "AnswerCallbackQueryRequest" => true,
                _ => throw new NotSupportedException(request.GetType().FullName)
            };

            return Task.FromResult((TResponse)response);
        }

        public Task<bool> TestApi(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHttpClientFactory : IHttpClientFactory {
        private readonly RecordingHttpMessageHandler _handler;

        public RecordingHttpClientFactory(params HttpResponseMessage[] responses) {
            _handler = new RecordingHttpMessageHandler(new Queue<HttpResponseMessage>(responses));
        }

        public IReadOnlyList<HttpRequestMessage> Requests => _handler.Requests;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);

        [ExcludeFromCodeCoverage]
        private sealed class RecordingHttpMessageHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler {
            private readonly Queue<HttpResponseMessage> _responses = responses;

            public List<HttpRequestMessage> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken) {
                Requests.Add(request);
                return Task.FromResult(_responses.Count > 0
                    ? _responses.Dequeue()
                    : new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
