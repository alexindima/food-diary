using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramOperationRecoveryTests {
    [Theory]
    [InlineData("Pending", "recognizing")]
    [InlineData("Failed", "failed")]
    public async Task RecognitionPolling_KeepsPendingWorkAndStopsFailedWork(string status, string expectedStage) {
        using var scenario = new BotOperationScenario();
        scenario.State = new BotPhotoCheckpoint("recognizing", ImageAssetId: scenario.ImageId, RecognitionId: scenario.Id);
        scenario.BusinessResponse = _ => Task.FromResult(BotOperationScenario.Json(new { id = scenario.Id, imageAssetId = scenario.ImageId, status }));
        await scenario.ProcessAsync();
        Assert.Equal(expectedStage, scenario.State.Stage);
        Assert.False(scenario.Completed);
    }

    [Fact]
    public async Task WaterWithoutDurableTimestamp_IsRejectedWithoutWritingEntry() {
        using var scenario = new BotOperationScenario { CreatedAtUtc = default };
        scenario.Incoming = scenario.Incoming with { Kind = "water", AmountMl = 250 };
        await scenario.ProcessAsync();
        Assert.Equal("failed", scenario.State.Stage);
        Assert.Equal("InvalidImageOrIdentity", scenario.State.ErrorCode);
        Assert.Single(scenario.BusinessPaths);
    }

    [Fact]
    public async Task UnknownCheckpointStage_DoesNotRestartBusinessWork() {
        using var scenario = new BotOperationScenario();
        scenario.State = new BotPhotoCheckpoint("future-stage");
        await scenario.ProcessAsync();
        Assert.True(scenario.Completed);
        Assert.Equal("OperationStateInvalid", scenario.State.ErrorCode);
        Assert.Empty(scenario.BusinessPaths);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Гречка с курицей\nСоус отдельно")]
    public async Task PremiumPhoto_AdvancesAcrossDurableCheckpointsAndDeliversSavedReceipt(string? caption) {
        using var scenario = new BotOperationScenario();
        scenario.Incoming = scenario.Incoming with { Caption = caption, Language = "ru" };
        int uploads = 0;
        int recognitions = 0;
        int saves = 0;
        scenario.BusinessResponse = async request => {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/upload-url", StringComparison.Ordinal)) {
                Assert.Equal($"telegram:{scenario.Id:N}:upload:0", Assert.Single(request.Headers.GetValues("Idempotency-Key")));
                return BotOperationScenario.Json(new BotImageUpload("https://storage.example/upload", "food", DateTime.UtcNow.AddMinutes(5), new BotImageAssetId(scenario.ImageId)));
            }
            if (string.Equals(path, "/upload", StringComparison.Ordinal)) {
                uploads++;
                Assert.Null(request.Headers.Authorization);
                Assert.Equal(new byte[] { 255, 216, 255 }, await request.Content!.ReadAsByteArrayAsync());
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            if (path.EndsWith("/confirm", StringComparison.Ordinal)) {
                Assert.Equal($"telegram:{scenario.ImageId:N}:confirm", Assert.Single(request.Headers.GetValues("Idempotency-Key")));
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            if (string.Equals(path, "/api/v1/ai/food/recognitions", StringComparison.Ordinal)) {
                recognitions++;
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                Assert.Equal(caption, body.RootElement.GetProperty("description").GetString());
                return BotOperationScenario.Json(new { id = scenario.Id, imageAssetId = scenario.ImageId, status = "Pending" });
            }
            if (path.StartsWith("/api/v1/ai/food/recognitions/", StringComparison.Ordinal)) {
                return BotOperationScenario.Json(new {
                    id = scenario.Id, imageAssetId = scenario.ImageId, status = "Succeeded",
                    nutrition = new { calories = 400, protein = 10, fat = 20, carbs = 30, items = new[] { new { name = "Гречка" }, new { name = "Курица" } } },
                });
            }
            if (path.StartsWith("/api/v1/meals/recognitions/", StringComparison.Ordinal)) {
                saves++;
                using JsonDocument body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
                Assert.Equal(scenario.Incoming.OccurredAtUtc, body.RootElement.GetProperty("occurredAtUtc").GetDateTime());
                return BotOperationScenario.Json(new BotRecognizedMeal(new BotOperationId(scenario.Id), new BotMealId(scenario.MealId), DateTime.UtcNow.AddMinutes(10), Undone: false));
            }
            throw new InvalidOperationException(path);
        };

        foreach (string stage in new[] { "upload", "image-ready", "recognizing", "recognition-ready", "meal-saved" }) {
            await scenario.ProcessAsync();
            Assert.Equal(stage, scenario.State.Stage);
            Assert.False(scenario.Completed);
            Assert.Empty(scenario.Notices);
            if (string.Equals(stage, "upload", StringComparison.Ordinal)) {
                scenario.IsPremium = false;
            }
        }
        await scenario.ProcessAsync();

        string notice = Assert.Single(scenario.Notices);
        JsonElement markup = Assert.IsType<JsonElement>(scenario.LastNoticeMarkup);
        JsonElement edit = Assert.Single(markup.GetProperty("inline_keyboard").EnumerateArray().SelectMany(row => row.EnumerateArray()),
            button => string.Equals(button.GetProperty("text").GetString(), "Редактировать", StringComparison.Ordinal));
        Assert.Multiple(
            () => Assert.True(scenario.Completed),
            () => Assert.Equal(1, uploads),
            () => Assert.Equal(1, recognitions),
            () => Assert.Equal(1, saves),
            () => Assert.Single(scenario.BusinessPaths, path => string.Equals(path, "/api/v1/billing/overview", StringComparison.Ordinal)),
            () => Assert.Contains("Приём пищи сохранён", notice, StringComparison.Ordinal),
            () => Assert.Contains("• Гречка\n• Курица", notice, StringComparison.Ordinal),
            () => Assert.Contains("400 ккал", notice, StringComparison.Ordinal),
            () => Assert.Equal($"https://diary.example/meals/{scenario.MealId:D}/edit", edit.GetProperty("web_app").GetProperty("url").GetString()));
    }

    [Theory]
    [InlineData("received", "ru", "Добавить вручную")]
    [InlineData("received", "en", "Add manually")]
    [InlineData("upload", "ru", "Добавить вручную")]
    [InlineData("image-ready", "en", "Add manually")]
    public async Task PhotoWithoutPremium_StopsBeforePhotoWorkAndOffersManualEntry(string stage, string language, string buttonText) {
        using var scenario = new BotOperationScenario { IsPremium = false };
        scenario.Incoming = scenario.Incoming with { Language = language };
        scenario.State = new BotPhotoCheckpoint(stage, Upload:
            string.Equals(stage, "upload", StringComparison.Ordinal)
                ? new BotImageUpload("https://storage.example/upload", "food", DateTime.UtcNow.AddMinutes(5), new BotImageAssetId(scenario.ImageId))
                : null, ImageAssetId: scenario.ImageId);

        await scenario.ProcessAsync();

        Assert.Multiple(
            () => Assert.Equal("failed", scenario.State.Stage),
            () => Assert.Equal(BotRecognitionFailure.PremiumRequired, scenario.State.ErrorCode),
            () => Assert.Null(scenario.State.Upload),
            () => Assert.Empty(scenario.Notices));

        await scenario.ProcessAsync();

        JsonElement markup = Assert.IsType<JsonElement>(scenario.LastNoticeMarkup);
        JsonElement button = Assert.Single(Assert.Single(markup.GetProperty("inline_keyboard").EnumerateArray()).EnumerateArray());
        Assert.Multiple(
            () => Assert.True(scenario.Completed),
            () => Assert.Contains("Food Diary Premium", Assert.Single(scenario.Notices), StringComparison.Ordinal),
            () => Assert.Equal(buttonText, button.GetProperty("text").GetString()),
            () => Assert.Equal("https://diary.example/meals/add", button.GetProperty("web_app").GetProperty("url").GetString()),
            () => Assert.Equal(new[] { "/api/v1/auth/telegram/bot/auth", "/api/v1/billing/overview" }, scenario.BusinessPaths, StringComparer.Ordinal));
    }

    [Fact]
    public async Task UnavailablePremiumStatus_LeavesPhotoPendingWithoutSendingAPremiumNotice() {
        using var scenario = new BotOperationScenario { BillingStatus = HttpStatusCode.ServiceUnavailable };

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(scenario.ProcessAsync);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode),
            () => Assert.Equal("received", scenario.State.Stage),
            () => Assert.Equal(0, scenario.Checkpoints),
            () => Assert.Empty(scenario.Notices),
            () => Assert.Equal(new[] { "/api/v1/auth/telegram/bot/auth", "/api/v1/billing/overview" }, scenario.BusinessPaths, StringComparer.Ordinal));
    }

    [Fact]
    public async Task LegacyPhoto_PersistsPremiumApprovalBeforeSubmittingRecognition() {
        using var scenario = new BotOperationScenario();
        scenario.State = new BotPhotoCheckpoint("image-ready", ImageAssetId: scenario.ImageId);

        await scenario.ProcessAsync();

        Assert.Equal(new BotPhotoCheckpoint("image-ready", ImageAssetId: scenario.ImageId, PhotoPremiumApproved: true), scenario.State);
        Assert.Equal(new[] { "/api/v1/auth/telegram/bot/auth", "/api/v1/billing/overview" }, scenario.BusinessPaths, StringComparer.Ordinal);

        scenario.IsPremium = false;
        scenario.BusinessResponse = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<HttpRequestException>(scenario.ProcessAsync);

        scenario.BusinessResponse = _ => Task.FromResult(BotOperationScenario.Json(new { id = scenario.Id, imageAssetId = scenario.ImageId, status = "Pending" }));
        await scenario.ProcessAsync();

        Assert.Equal("recognizing", scenario.State.Stage);
        Assert.Single(scenario.BusinessPaths, path => string.Equals(path, "/api/v1/billing/overview", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExpiredUpload_StartsANewAttemptWithoutUploadingToTheExpiredUrl() {
        using var scenario = new BotOperationScenario();
        scenario.State = new BotPhotoCheckpoint("upload", UploadAttempt: 2,
            Upload: new BotImageUpload("https://storage.example/expired", "food", DateTime.UtcNow.AddMinutes(-1), new BotImageAssetId(scenario.ImageId)));
        await scenario.ProcessAsync();
        Assert.Equal(new BotPhotoCheckpoint(UploadAttempt: 3, PhotoPremiumApproved: true), scenario.State);
        Assert.Equal(new[] { "/api/v1/auth/telegram/bot/auth", "/api/v1/billing/overview" }, scenario.BusinessPaths, StringComparer.Ordinal);
        Assert.False(scenario.Completed);
    }

    [Fact]
    public async Task LeaseConflict_DoesNotStartBusinessWorkOrOverwriteTheOtherWorkersCheckpoint() {
        using var scenario = new BotOperationScenario { LeaseConflict = true };
        await scenario.ProcessAsync();
        Assert.Empty(scenario.BusinessPaths);
        Assert.Equal(0, scenario.Checkpoints);
    }

    [Theory]
    [InlineData("Ai.ConsentRequired", HttpStatusCode.Forbidden)]
    [InlineData("Ai.QuotaExceeded", HttpStatusCode.TooManyRequests)]
    [InlineData("BadRequest", HttpStatusCode.BadRequest)]
    [InlineData("NotFound", HttpStatusCode.NotFound)]
    public async Task PermanentFailure_IsCheckpointedBeforeSendingNotice(string code, HttpStatusCode status) {
        using var scenario = new BotOperationScenario();
        scenario.State = new BotPhotoCheckpoint("image-ready", ImageAssetId: scenario.ImageId, PhotoPremiumApproved: true);
        scenario.BusinessResponse = _ => Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(new { error = code }) });
        await scenario.ProcessAsync();
        Assert.Equal("failed", scenario.State.Stage);
        Assert.Equal(code, scenario.State.ErrorCode);
        Assert.False(scenario.Completed);
        Assert.Empty(scenario.Notices);
        int calls = scenario.BusinessPaths.Count;
        await scenario.ProcessAsync();
        Assert.True(scenario.Completed);
        Assert.Single(scenario.Notices);
        Assert.Equal(calls, scenario.BusinessPaths.Count);
    }

    [Theory]
    [InlineData("photo", "ru", "Ai.ConsentRequired", "согласие")]
    [InlineData("photo", "en", "Ai.ConsentRequired", "consent")]
    [InlineData("photo", "ru", "Ai.QuotaExceeded", "лимит")]
    [InlineData("photo", "en", "Ai.QuotaExceeded", "quota")]
    [InlineData("photo", "ru", "Unknown", "Не удалось добавить еду")]
    [InlineData("water", "ru", "Unknown", "Не удалось добавить воду")]
    [InlineData("water", "en", "Unknown", "Could not add water")]
    [InlineData("statistics", "ru", "Unknown", "Не удалось получить статистику")]
    [InlineData("statistics", "en", "Unknown", "Could not load statistics")]
    [InlineData("meal-undo", "ru", "Unknown", "Не удалось отменить")]
    [InlineData("meal-undo", "en", "Unknown", "Could not undo")]
    public async Task FailedOperation_UsesLocalizedNoticeAndTerminatesWhenBotIsBlocked(string kind, string language, string code, string expected) {
        using var scenario = new BotOperationScenario { BlockedByUser = true };
        scenario.Incoming = scenario.Incoming with { Kind = kind, Language = language };
        scenario.State = new BotPhotoCheckpoint("failed", ErrorCode: code);
        await scenario.ProcessAsync();
        Assert.True(scenario.Completed);
        Assert.Empty(scenario.BusinessPaths);
        Assert.Contains(expected, Assert.Single(scenario.Notices), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ru", "Undone", "Запись отменена")]
    [InlineData("en", "AlreadyDeleted", "Entry undone")]
    [InlineData("ru", "Meal.RecognitionUndoExpired", "Срок отмены истёк")]
    [InlineData("en", "Meal.RecognitionUndoExpired", "undo period expired")]
    [InlineData("ru", "Meal.RecognitionUndoChanged", "Еда уже отредактирована")]
    [InlineData("ru", "Unknown", "недоступна для отмены")]
    [InlineData("en", "Unknown", "not available to undo")]
    public async Task CompletedUndo_IsNotRepeatedEvenWhenNotificationIsBlocked(string language, string result, string expected) {
        using var scenario = new BotOperationScenario { BlockedByUser = true };
        scenario.Incoming = scenario.Incoming with { Kind = "meal-undo", Language = language };
        scenario.State = new BotPhotoCheckpoint("undo-complete", ErrorCode: result);
        await scenario.ProcessAsync();
        Assert.True(scenario.Completed);
        Assert.Empty(scenario.BusinessPaths);
        Assert.Contains(expected, Assert.Single(scenario.Notices), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("en")]
    public async Task AlreadyUndoneMeal_DoesNotOfferUndoOrReportItAsANewMeal(string language) {
        using var scenario = new BotOperationScenario();
        scenario.Incoming = scenario.Incoming with { Language = language };
        scenario.State = new BotPhotoCheckpoint("meal-saved", SavedMeal: new BotRecognizedMeal(new BotOperationId(scenario.Id), new BotMealId(scenario.MealId), DateTime.UtcNow, Undone: true));
        await scenario.ProcessAsync();
        Assert.True(scenario.Completed);
        Assert.Contains(string.Equals(language, "ru", StringComparison.Ordinal) ? "уже отменена" : "already been undone", Assert.Single(scenario.Notices), StringComparison.Ordinal);
        Assert.Empty(scenario.BusinessPaths);
    }

    [Theory]
    [InlineData("image-ready")]
    [InlineData("recognizing")]
    public async Task MismatchedRecognitionIdentity_FailsInsteadOfSavingSomeoneElsesResult(string stage) {
        using var scenario = new BotOperationScenario();
        scenario.State = new BotPhotoCheckpoint(stage, ImageAssetId: scenario.ImageId, RecognitionId: scenario.Id, PhotoPremiumApproved: true);
        scenario.BusinessResponse = _ => Task.FromResult(BotOperationScenario.Json(new { id = Guid.NewGuid(), imageAssetId = scenario.ImageId, status = "Succeeded" }));
        await scenario.ProcessAsync();
        Assert.Equal("failed", scenario.State.Stage);
        Assert.Equal("InvalidImageOrIdentity", scenario.State.ErrorCode);
        Assert.DoesNotContain(scenario.BusinessPaths, path => path.StartsWith("/api/v1/meals/", StringComparison.Ordinal));
    }
}
