using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
internal sealed class BotOperationScenario : IDisposable {
    private readonly ScenarioHandler _handler;
    private readonly HttpClient _telegramHttp;
    private readonly HttpClient _operationHttp;
    private readonly TelegramOperationWorker _worker;
    private readonly BotOperationClient _operations;

    public BotOperationScenario() {
        _handler = new ScenarioHandler(RespondAsync);
        _telegramHttp = new HttpClient(_handler, disposeHandler: false);
        _operationHttp = new HttpClient(_handler, disposeHandler: false);
        IOptions<TelegramBotOptions> options = Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://diary.example", ApiSecret = "test-secret", WebAppUrl = "https://diary.example" });
        _operations = new BotOperationClient(_operationHttp, options);
        _worker = new TelegramOperationWorker(new ScenarioFactory(_handler), options,
            new TelegramBotClient("123:test", _telegramHttp), TimeProvider.System, NullLogger<TelegramOperationWorker>.Instance);
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Guid UserId { get; } = Guid.NewGuid();
    public Guid ImageId { get; } = Guid.NewGuid();
    public Guid MealId { get; } = Guid.NewGuid();
    public BotIncomingOperation Incoming { get; set; } = new("photo", 123, 123, 10, DateTime.UtcNow, "en", "food", "image/jpeg");
    public BotPhotoCheckpoint State { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool LeaseConflict { get; set; }
    public bool BlockedByUser { get; set; }
    public bool Completed { get; private set; }
    public int Checkpoints { get; private set; }
    public List<string> Notices { get; } = [];
    public List<string> BusinessPaths { get; } = [];
    public Func<HttpRequestMessage, Task<HttpResponseMessage>>? BusinessResponse { get; set; }

    public Task ProcessAsync() => _worker.ProcessAsync(_operations, Id, CancellationToken.None);
    public static HttpResponseMessage Json<T>(T body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    private async Task<HttpResponseMessage> RespondAsync(HttpRequestMessage request) {
        string path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/lease", StringComparison.Ordinal)) {
            return LeaseConflict ? new HttpResponseMessage(HttpStatusCode.Conflict) : Json(new BotOperationLease(Id, Guid.NewGuid(), UserId, 1,
                JsonSerializer.Serialize(Incoming), JsonSerializer.Serialize(State), DateTime.UtcNow.AddMinutes(2), CreatedAtUtc));
        }
        if (path.EndsWith("/checkpoint", StringComparison.Ordinal)) {
            using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
            State = JsonSerializer.Deserialize<BotPhotoCheckpoint>(body.RootElement.GetProperty("checkpoint").GetString()!)!;
            Completed = body.RootElement.GetProperty("completed").GetBoolean();
            Checkpoints++;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
        if (path.EndsWith("/sendMessage", StringComparison.OrdinalIgnoreCase)) {
            using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
            Notices.Add(body.RootElement.GetProperty("text").GetString()!);
            return BlockedByUser
                ? new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = JsonContent.Create(new { ok = false, error_code = 403, description = "Forbidden: bot was blocked" }) }
                : Json(new { ok = true, result = new { message_id = 77, date = 1789214400, chat = new { id = 123, type = "private" }, text = "Delivered" } });
        }
        BusinessPaths.Add(path);
        if (path.EndsWith("/bot/auth", StringComparison.Ordinal)) {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"security_version\":\"1\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return Json(new { AccessToken = $"header.{payload}.signature", User = new { Id = UserId } });
        }
        if (path.EndsWith("/getFile", StringComparison.OrdinalIgnoreCase)) {
            return Json(new { ok = true, result = new { file_id = "food", file_unique_id = "unique", file_size = 3, file_path = "photos/food" } });
        }
        if (path.EndsWith("/photos/food", StringComparison.Ordinal)) {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([255, 216, 255]) };
        }
        return BusinessResponse is not null ? await BusinessResponse(request) : throw new InvalidOperationException($"Unexpected business request: {path}");
    }

    public void Dispose() {
        _worker.Dispose();
        _operationHttp.Dispose();
        _telegramHttp.Dispose();
        _handler.Dispose();
    }

    [ExcludeFromCodeCoverage]
    private sealed class ScenarioHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ScenarioFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
