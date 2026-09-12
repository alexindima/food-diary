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
public sealed class BotPhotoPipelineTests {
    [Theory]
    [InlineData("{broken", null, false)]
    [InlineData("null", null, false)]
    [InlineData("{\"Kind\":\"future\",\"ChatId\":123,\"TelegramUserId\":123,\"MessageId\":10}", null, false)]
    [InlineData(null, "{broken", true)]
    [InlineData(null, "{\"Stage\":\"meal-saved\"}", true)]
    public async Task InvalidState_CompletesWithoutRepeatingBusinessWork(string? invalidInput, string? checkpoint, bool expectedNotice) {
        var operationId = Guid.NewGuid();
        var incoming = new BotIncomingOperation("photo", 123, 123, 10, DateTime.UtcNow, "en");
        var lease = new BotOperationLease(operationId, Guid.NewGuid(), Guid.NewGuid(), 1,
            invalidInput ?? JsonSerializer.Serialize(incoming), checkpoint, DateTime.UtcNow.AddMinutes(2));
        int notices = 0;
        bool completed = false;
        using var handler = new Handler(async request => {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/lease", StringComparison.Ordinal)) {
                return Json(lease);
            }
            if (path.EndsWith("/sendMessage", StringComparison.OrdinalIgnoreCase)) {
                notices++;
                Assert.Contains("may already have been saved", await request.Content!.ReadAsStringAsync(), StringComparison.Ordinal);
                return Json(new { ok = true, result = new { message_id = 77, date = 1789214400, chat = new { id = 123, type = "private" }, text = "Check your diary" } });
            }
            if (path.EndsWith("/checkpoint", StringComparison.Ordinal)) {
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                completed = body.RootElement.GetProperty("completed").GetBoolean();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            throw new InvalidOperationException("Invalid state must not invoke authentication, AI or meal APIs.");
        });
        IOptions<TelegramBotOptions> options = Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://api.example.com", ApiSecret = "test-operation-secret" });
        var factory = new Factory(handler);
        using var telegramHttp = new HttpClient(handler, disposeHandler: false);
        var worker = new TelegramOperationWorker(factory, options, new TelegramBotClient("123:test", telegramHttp), TimeProvider.System,
            NullLogger<TelegramOperationWorker>.Instance);
        using HttpClient operationHttp = factory.CreateClient(BotOperationClient.ClientName);

        await worker.ProcessAsync(new BotOperationClient(operationHttp, options), operationId, CancellationToken.None);

        Assert.True(completed);
        Assert.Equal(expectedNotice ? 1 : 0, notices);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Statistics_ResumesSavedSummaryWithoutReadingAgain(bool notificationOnly) {
        var operationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var incoming = new BotIncomingOperation("statistics", 123, 123, 10, OccurredAtUtc: null, "en", PeriodDays: 1);
        var summary = new BotDiaryStatistics("UTC", 1, 1, 1234, 10, 20, 30, 4, 250, 1, 1234, 2000,
            [new BotDiaryStatisticsDay(new DateOnly(2026, 9, 12), 1234, 10, 20, 30, 4, 250, 1, 2100)]);
        var checkpoint = new BotPhotoCheckpoint(notificationOnly ? "statistics-ready" : "received", Statistics: notificationOnly ? summary : null);
        var lease = new BotOperationLease(operationId, Guid.NewGuid(), userId, 1, JsonSerializer.Serialize(incoming),
            JsonSerializer.Serialize(checkpoint), DateTime.UtcNow.AddMinutes(2));
        int reads = 0;
        int notices = 0;
        bool? completed = null;
        using var handler = new Handler(async request => {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/lease", StringComparison.Ordinal)) {
                return Json(lease);
            }
            if (path.EndsWith("/bot/auth", StringComparison.Ordinal)) {
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"security_version\":\"1\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                return Json(new { AccessToken = $"header.{payload}.signature", User = new { Id = userId } });
            }
            if (string.Equals(path, "/api/v1/statistics/diary-summary", StringComparison.Ordinal)) {
                reads++;
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("?days=1", request.RequestUri.Query);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                return Json(summary);
            }
            if (path.EndsWith("/sendMessage", StringComparison.OrdinalIgnoreCase)) {
                notices++;
                Assert.Contains("1234 / 2100", await request.Content!.ReadAsStringAsync(), StringComparison.Ordinal);
                return Json(new { ok = true, result = new { message_id = 77, date = 1789214400, chat = new { id = 123, type = "private" }, text = "Statistics" } });
            }
            if (path.EndsWith("/checkpoint", StringComparison.Ordinal)) {
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                completed = body.RootElement.GetProperty("completed").GetBoolean();
                BotPhotoCheckpoint? saved = JsonSerializer.Deserialize<BotPhotoCheckpoint>(body.RootElement.GetProperty("checkpoint").GetString()!);
                Assert.Equal("statistics-ready", saved?.Stage);
                Assert.Equal(1234, saved?.Statistics?.TotalCalories);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            throw new InvalidOperationException("Unexpected statistics request.");
        });
        IOptions<TelegramBotOptions> options = Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://api.example.com", ApiSecret = "test-operation-secret" });
        var factory = new Factory(handler);
        using var telegramHttp = new HttpClient(handler, disposeHandler: false);
        var worker = new TelegramOperationWorker(factory, options, new TelegramBotClient("123:test", telegramHttp), TimeProvider.System,
            NullLogger<TelegramOperationWorker>.Instance);
        using HttpClient operationHttp = factory.CreateClient(BotOperationClient.ClientName);
        await worker.ProcessAsync(new BotOperationClient(operationHttp, options), operationId, CancellationToken.None);
        Assert.Multiple(
            () => Assert.Equal(notificationOnly ? 0 : 1, reads),
            () => Assert.Equal(notificationOnly ? 1 : 0, notices),
            () => Assert.Equal(notificationOnly, completed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Water_UsesDurableTimestampAndDoesNotRepeatSavedMutation(bool notificationOnly) {
        var operationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        DateTime createdAt = new(2026, 9, 10, 23, 59, 0, DateTimeKind.Utc);
        var incoming = new BotIncomingOperation("water", 123, 123, 10, OccurredAtUtc: null, "en", AmountMl: 250);
        var checkpoint = new BotPhotoCheckpoint(notificationOnly ? "water-saved" : "received", WaterEntryId: notificationOnly ? entryId : null);
        var lease = new BotOperationLease(operationId, Guid.NewGuid(), userId, 1, JsonSerializer.Serialize(incoming),
            JsonSerializer.Serialize(checkpoint), DateTime.UtcNow.AddMinutes(2), createdAt);
        int mutations = 0;
        int notices = 0;
        bool? completed = null;
        using var handler = new Handler(async request => {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/lease", StringComparison.Ordinal)) {
                return Json(lease);
            }
            if (path.EndsWith("/bot/auth", StringComparison.Ordinal)) {
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"security_version\":\"1\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                return Json(new { AccessToken = $"header.{payload}.signature", User = new { Id = userId } });
            }
            if (path.StartsWith("/api/v1/hydrations/operations/", StringComparison.Ordinal)) {
                mutations++;
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                Assert.Equal(createdAt, body.RootElement.GetProperty("timestampUtc").GetDateTime());
                Assert.Equal(250, body.RootElement.GetProperty("amountMl").GetInt32());
                return Json(new BotHydrationReceipt(operationId, entryId, createdAt, 250));
            }
            if (path.EndsWith("/sendMessage", StringComparison.OrdinalIgnoreCase)) {
                notices++;
                Assert.Contains("250 ml", await request.Content!.ReadAsStringAsync(), StringComparison.Ordinal);
                return Json(new { ok = true, result = new { message_id = 77, date = 1789214400, chat = new { id = 123, type = "private" }, text = "Water" } });
            }
            if (path.EndsWith("/checkpoint", StringComparison.Ordinal)) {
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                completed = body.RootElement.GetProperty("completed").GetBoolean();
                BotPhotoCheckpoint? saved = JsonSerializer.Deserialize<BotPhotoCheckpoint>(body.RootElement.GetProperty("checkpoint").GetString()!);
                Assert.Equal("water-saved", saved?.Stage);
                Assert.Equal(entryId, saved?.WaterEntryId);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            throw new InvalidOperationException("Unexpected water request.");
        });
        IOptions<TelegramBotOptions> options = Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://api.example.com", ApiSecret = "test-operation-secret" });
        var factory = new Factory(handler);
        using var telegramHttp = new HttpClient(handler, disposeHandler: false);
        var worker = new TelegramOperationWorker(factory, options, new TelegramBotClient("123:test", telegramHttp), TimeProvider.System,
            NullLogger<TelegramOperationWorker>.Instance);
        using HttpClient operationHttp = factory.CreateClient(BotOperationClient.ClientName);
        await worker.ProcessAsync(new BotOperationClient(operationHttp, options), operationId, CancellationToken.None);
        Assert.Equal(notificationOnly ? 0 : 1, mutations);
        Assert.Equal(notificationOnly ? 1 : 0, notices);
        Assert.Equal(notificationOnly, completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Undo_StoresConflictAndResumesNotificationWithoutRepeatingMutation(bool notificationOnly) {
        var operationId = Guid.NewGuid();
        var mealOperationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var incoming = new BotIncomingOperation("meal-undo", 123, 123, 10, OccurredAtUtc: null, "en", MealOperationId: mealOperationId);
        var checkpoint = new BotPhotoCheckpoint(notificationOnly ? "undo-complete" : "received", ErrorCode: "Meal.RecognitionUndoChanged");
        var lease = new BotOperationLease(operationId, Guid.NewGuid(), userId, 1, JsonSerializer.Serialize(incoming),
            JsonSerializer.Serialize(checkpoint), DateTime.UtcNow.AddMinutes(2));
        int mutations = 0;
        int notices = 0;
        bool? completed = null;
        BotPhotoCheckpoint? saved = null;
        using var handler = new Handler(async request => {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/lease", StringComparison.Ordinal)) {
                return Json(lease);
            }
            if (path.EndsWith("/bot/auth", StringComparison.Ordinal)) {
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"security_version\":\"1\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                return Json(new { AccessToken = $"header.{payload}.signature", User = new { Id = userId } });
            }
            if (path.EndsWith("/undo", StringComparison.Ordinal)) {
                mutations++;
                Assert.Equal($"/api/v1/meals/recognitions/{mealOperationId:D}/undo", path);
                Assert.NotNull(request.Headers.Authorization);
                return new HttpResponseMessage(HttpStatusCode.Conflict) { Content = JsonContent.Create(new { error = "Meal.RecognitionUndoChanged" }) };
            }
            if (path.EndsWith("/sendMessage", StringComparison.OrdinalIgnoreCase)) {
                notices++;
                Assert.Contains("This meal was edited", await request.Content!.ReadAsStringAsync(), StringComparison.Ordinal);
                return Json(new { ok = true, result = new { message_id = 77, date = 1789214400, chat = new { id = 123, type = "private" }, text = "Edited" } });
            }
            if (path.EndsWith("/checkpoint", StringComparison.Ordinal)) {
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                completed = body.RootElement.GetProperty("completed").GetBoolean();
                saved = JsonSerializer.Deserialize<BotPhotoCheckpoint>(body.RootElement.GetProperty("checkpoint").GetString()!);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            throw new InvalidOperationException("Unexpected undo request.");
        });
        IOptions<TelegramBotOptions> options = Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://api.example.com", ApiSecret = "test-operation-secret" });
        var factory = new Factory(handler);
        using var telegramHttp = new HttpClient(handler, disposeHandler: false);
        var worker = new TelegramOperationWorker(factory, options, new TelegramBotClient("123:test", telegramHttp), TimeProvider.System,
            NullLogger<TelegramOperationWorker>.Instance);
        using HttpClient operationHttp = factory.CreateClient(BotOperationClient.ClientName);

        await worker.ProcessAsync(new BotOperationClient(operationHttp, options), operationId, CancellationToken.None);

        Assert.Equal(notificationOnly ? 0 : 1, mutations);
        Assert.Equal(notificationOnly ? 1 : 0, notices);
        Assert.Equal(notificationOnly, completed);
        Assert.Equal("undo-complete", saved?.Stage);
        Assert.Equal("Meal.RecognitionUndoChanged", saved?.ErrorCode);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ReadyOrSavedPhoto_ResumesWithoutRestartingRecognition(bool alreadySaved, bool loseFirstSaveResponse) {
        var operationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        DateTime occurredAt = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        var incoming = new BotIncomingOperation("photo", 123, 123, 10, occurredAt, "ru", "file", "image/jpeg");
        var meal = new BotRecognizedMeal(operationId, mealId, DateTime.UtcNow.AddHours(24), Undone: false);
        var checkpoint = new BotPhotoCheckpoint(alreadySaved ? "meal-saved" : "recognition-ready", RecognitionId: operationId,
            SavedMeal: alreadySaved ? meal : null, Nutrition: new BotMealNutrition(432.1m, 12m, 13m, 14m));
        var lease = new BotOperationLease(operationId, Guid.NewGuid(), userId, 1, JsonSerializer.Serialize(incoming),
            JsonSerializer.Serialize(checkpoint), DateTime.UtcNow.AddMinutes(2));
        int creates = 0;
        int notices = 0;
        bool? completed = null;
        BotPhotoCheckpoint? saved = null;
        using var handler = new Handler(async request => {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/lease", StringComparison.Ordinal)) {
                return Json(lease);
            }
            if (path.EndsWith("/bot/auth", StringComparison.Ordinal)) {
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"security_version\":\"1\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                return Json(new { AccessToken = $"header.{payload}.signature", User = new { Id = userId } });
            }
            if (path.StartsWith("/api/v1/meals/recognitions/", StringComparison.Ordinal)) {
                creates++;
                Assert.Contains(operationId.ToString(), path, StringComparison.Ordinal);
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                Assert.Equal(occurredAt, body.RootElement.GetProperty("occurredAtUtc").GetDateTime());
                Assert.NotNull(request.Headers.Authorization);
                if (loseFirstSaveResponse && creates == 1) {
                    throw new HttpRequestException("The saved receipt response was lost.");
                }
                return Json(meal);
            }
            if (path.EndsWith("/sendMessage", StringComparison.OrdinalIgnoreCase)) {
                notices++;
                string body = await request.Content!.ReadAsStringAsync();
                Assert.Contains($"meal:undo:{operationId:N}", body, StringComparison.Ordinal);
                Assert.Contains("432.1", body, StringComparison.Ordinal);
                Assert.Contains("stats:today", body, StringComparison.Ordinal);
                return Json(new { ok = true, result = new { message_id = 77, date = 1789214400, chat = new { id = 123, type = "private" }, text = "Saved" } });
            }
            if (path.EndsWith("/checkpoint", StringComparison.Ordinal)) {
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                completed = body.RootElement.GetProperty("completed").GetBoolean();
                saved = JsonSerializer.Deserialize<BotPhotoCheckpoint>(body.RootElement.GetProperty("checkpoint").GetString()!);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            throw new InvalidOperationException("Unexpected photo completion request.");
        });
        IOptions<TelegramBotOptions> options = Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://api.example.com", ApiSecret = "test-operation-secret" });
        var factory = new Factory(handler);
        using var telegramHttp = new HttpClient(handler, disposeHandler: false);
        var worker = new TelegramOperationWorker(factory, options, new TelegramBotClient("123:test", telegramHttp),
            TimeProvider.System, NullLogger<TelegramOperationWorker>.Instance);
        using HttpClient operationHttp = factory.CreateClient(BotOperationClient.ClientName);

        if (loseFirstSaveResponse) {
            await Assert.ThrowsAsync<HttpRequestException>(() => worker.ProcessAsync(new BotOperationClient(operationHttp, options), operationId, CancellationToken.None));
            Assert.Null(saved);
            Assert.Equal(0, notices);
            worker = new TelegramOperationWorker(factory, options, new TelegramBotClient("123:test", telegramHttp),
                TimeProvider.System, NullLogger<TelegramOperationWorker>.Instance);
        }
        await worker.ProcessAsync(new BotOperationClient(operationHttp, options), operationId, CancellationToken.None);

        int expectedCreates = alreadySaved ? 0 : 1;
        if (loseFirstSaveResponse) {
            expectedCreates++;
        }
        Assert.Equal(expectedCreates, creates);
        Assert.Equal(alreadySaved ? 1 : 0, notices);
        Assert.Equal(alreadySaved, completed);
        Assert.Equal("meal-saved", saved?.Stage);
        Assert.Equal(mealId, saved?.SavedMeal?.MealId);
        Assert.Equal(432.1m, saved?.Nutrition?.Calories);
    }

    [Fact]
    public async Task ResumedRecognition_OnlyReadsExistingJobAndPersistsReadyStage() {
        var operationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var incoming = new BotIncomingOperation("photo", 123, 123, 10, DateTime.UtcNow, "ru", "file", "image/jpeg");
        var checkpoint = new BotPhotoCheckpoint("recognizing", ImageAssetId: imageId, RecognitionId: operationId);
        var lease = new BotOperationLease(operationId, Guid.NewGuid(), userId, 1, JsonSerializer.Serialize(incoming),
            JsonSerializer.Serialize(checkpoint), DateTime.UtcNow.AddMinutes(2));
        BotPhotoCheckpoint? saved = null;
        int reads = 0;
        using var handler = new Handler(async request => {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/lease", StringComparison.Ordinal)) {
                return Json(lease);
            }
            if (path.EndsWith("/bot/auth", StringComparison.Ordinal)) {
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"security_version\":\"1\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                return Json(new { AccessToken = $"header.{payload}.signature", User = new { Id = userId } });
            }
            if (path.Contains("/ai/food/recognitions/", StringComparison.Ordinal)) {
                Assert.Equal(HttpMethod.Get, request.Method);
                reads++;
                return Json(new {
                    id = operationId, imageAssetId = imageId, status = "Succeeded",
                    nutrition = new { calories = 432.1m, protein = 12m, fat = 13m, carbs = 14m },
                });
            }
            if (path.EndsWith("/checkpoint", StringComparison.Ordinal)) {
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                saved = JsonSerializer.Deserialize<BotPhotoCheckpoint>(body.RootElement.GetProperty("checkpoint").GetString()!);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            throw new InvalidOperationException("Unexpected request in resumed recognition test.");
        });
        IOptions<TelegramBotOptions> options = Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://api.example.com", ApiSecret = "test-operation-secret" });
        var factory = new Factory(handler);
        using var telegramHttp = new HttpClient(handler, disposeHandler: false);
        var bot = new TelegramBotClient("123:test", telegramHttp);
        var worker = new TelegramOperationWorker(factory, options, bot, TimeProvider.System, NullLogger<TelegramOperationWorker>.Instance);
        using HttpClient operationHttp = factory.CreateClient(BotOperationClient.ClientName);
        await worker.ProcessAsync(new BotOperationClient(operationHttp, options), operationId, CancellationToken.None);
        Assert.Equal(1, reads);
        Assert.Equal("recognition-ready", saved?.Stage);
        Assert.Equal(new BotMealNutrition(432.1m, 12m, 13m, 14m), saved?.Nutrition);
    }

    [Fact]
    public async Task StorageUpload_DoesNotIncludeAccountOrBotCredentials() {
        using var handler = new Handler(request => {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Null(request.Headers.Authorization);
            Assert.False(request.Headers.Contains("X-Telegram-Bot-Secret"));
            Assert.Equal("storage.example.com", request.RequestUri!.Host);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var http = new HttpClient(handler);
        var client = new BotDiaryClient(http, Options.Create(new TelegramBotOptions { ApiSecret = "test-operation-secret" }));
        await client.UploadAsync(new BotImageUpload("https://storage.example.com/upload", "image", DateTime.UtcNow.AddMinutes(5), Guid.NewGuid()),
            "image/jpeg", [255, 216, 255], CancellationToken.None);
    }

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    [ExcludeFromCodeCoverage]
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
