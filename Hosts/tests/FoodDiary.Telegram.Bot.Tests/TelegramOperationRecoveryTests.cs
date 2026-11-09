using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;
using Telegram.Bot.Exceptions;

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
        DateTime before = DateTime.UtcNow;
        await scenario.ProcessAsync();
        Assert.Equal(expectedStage, scenario.State.Stage);
        Assert.False(scenario.Completed);
        Assert.True(scenario.NextAttemptAtUtc < before.AddSeconds(4) == string.Equals(status, "Failed", StringComparison.Ordinal),
            "Expected recognition polling to wait only when the result is still pending.");
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
    [InlineData(null, "ru", "🔍 Распознаю продукты на фото…", false, false, true)]
    [InlineData("Гречка с курицей\nСоус отдельно", "ru", "🔍 Распознаю продукты на фото…", false, false, false)]
    [InlineData(null, "en", "🔍 Identifying foods in the photo…", true, false, true)]
    [InlineData("Original photo caption", "en", "🔍 Identifying foods in the photo…", false, true, true)]
    public async Task PremiumPhoto_AdvancesAcrossDurableCheckpointsAndDeliversSavedReceipt(
        string? caption, string language, string progressNotice, bool rejectProgressNotice, bool startTrial, bool hasConsent) {
        using var scenario = new BotOperationScenario { IsPremium = !startTrial, CanStartTrial = startTrial, HasAiConsent = hasConsent };
        bool russian = string.Equals(language, "ru", StringComparison.Ordinal);
        scenario.Incoming = scenario.Incoming with { Caption = caption, Language = language };
        scenario.BlockedByUser = rejectProgressNotice;
        int uploads = 0;
        int recognitions = 0;
        int saves = 0;
        int trialStarts = 0;
        scenario.BusinessResponse = async request => {
            string path = request.RequestUri!.AbsolutePath;
            if (string.Equals(path, "/api/v1/billing/trial", StringComparison.Ordinal)) {
                trialStarts++;
                Assert.Equal($"telegram:{scenario.Id:N}:trial", Assert.Single(request.Headers.GetValues("Idempotency-Key")));
                scenario.IsPremium = true;
                scenario.CanStartTrial = false;
                return BotOperationScenario.Json(new { isPremium = true, premiumTrialActive = true });
            }
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
                    id = scenario.Id,
                    imageAssetId = scenario.ImageId,
                    status = "Succeeded",
                    vision = new { items = new[] { new { nameEn = "Chicken", nameLocal = "Курица" }, new { nameEn = "Buckwheat", nameLocal = "Гречка" } } },
                    nutrition = new {
                        calories = 400,
                        protein = 10,
                        fat = 20,
                        carbs = 30,
                        items = new[] { new { name = "buckwheat", amount = 150, unit = "g", calories = 300 }, new { name = "Chicken", amount = 1, unit = "pcs", calories = 100 } },
                    },
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

        if (startTrial) {
            await scenario.ProcessAsync();
            await scenario.ProcessAsync();
            Assert.Equal(BotPhotoTrial.Offered, scenario.State.Stage);
            Assert.Contains("No card or automatic renewal", Assert.Single(scenario.Notices), StringComparison.Ordinal);
            JsonElement trialMarkup = Assert.IsType<JsonElement>(scenario.LastNoticeMarkup);
            JsonElement trialButton = Assert.Single(trialMarkup.GetProperty("inline_keyboard")[0].EnumerateArray());
            Assert.Equal($"photo-trial:{scenario.Id:N}", trialButton.GetProperty("callback_data").GetString());
            Assert.Equal(0, trialStarts);
            Assert.DoesNotContain(scenario.BusinessPaths, path => path.EndsWith("/getFile", StringComparison.OrdinalIgnoreCase));
            Assert.True(await scenario.AcceptTrialAsync());
            Assert.False(await scenario.AcceptTrialAsync());
            await scenario.ProcessAsync();
            Assert.Equal("received", scenario.State.Stage);
            Assert.Equal(1, trialStarts);
            Assert.DoesNotContain(scenario.BusinessPaths, path => path.EndsWith("/getFile", StringComparison.OrdinalIgnoreCase));
            scenario.Notices.Clear();
            scenario.SilentNotices.Clear();
        }
        if (!hasConsent) {
            await scenario.ProcessAsync();
            await scenario.ProcessAsync();
            Assert.Equal(BotPhotoCallback.WaitingForConsent, scenario.State.Stage);
            Assert.False(scenario.Completed);
            Assert.Equal(scenario.CreatedAtUtc.AddMinutes(15), scenario.NextAttemptAtUtc);
            Assert.DoesNotContain(scenario.BusinessPaths, path => path.EndsWith("/getFile", StringComparison.OrdinalIgnoreCase));
            JsonElement consentMarkup = Assert.IsType<JsonElement>(scenario.LastNoticeMarkup);
            JsonElement settings = Assert.Single(consentMarkup.GetProperty("inline_keyboard")[0].EnumerateArray());
            Assert.Equal("https://diary.example/privacy-settings", settings.GetProperty("web_app").GetProperty("url").GetString());
            JsonElement resume = Assert.Single(consentMarkup.GetProperty("inline_keyboard")[1].EnumerateArray());
            string? callback = resume.GetProperty("callback_data").GetString();
            Assert.Equal($"photo-continue:{scenario.Id:N}", callback);
            Assert.True(await scenario.ResumePhotoAsync(callback));
            await scenario.ProcessAsync();
            await scenario.ProcessAsync();
            Assert.Equal(BotPhotoCallback.WaitingForConsent, scenario.State.Stage);
            Assert.DoesNotContain(scenario.BusinessPaths, path => path.EndsWith("/getFile", StringComparison.OrdinalIgnoreCase));
            scenario.HasAiConsent = true;
            Assert.True(await scenario.ResumePhotoAsync(callback));
            Assert.False(await scenario.ResumePhotoAsync(callback));
            scenario.Notices.Clear();
            scenario.SilentNotices.Clear();
        }

        foreach (string stage in new[] { "upload", "image-ready", "recognizing", "recognition-ready", "meal-saved" }) {
            DateTime before = DateTime.UtcNow;
            await scenario.ProcessAsync();
            Assert.Equal(stage, scenario.State.Stage);
            Assert.False(scenario.Completed);
            Assert.True(scenario.Notices.Count == 1, "Expected one photo processing acknowledgement before recognition.");
            Assert.Equal(progressNotice, scenario.Notices[0]);
            Assert.True(Assert.Single(scenario.SilentNotices), "Expected the photo progress message to be silent.");
            if (!string.Equals(stage, "recognizing", StringComparison.Ordinal)) {
                Assert.True(scenario.NextAttemptAtUtc < before.AddSeconds(4), "Expected a ready stage without a polling delay.");
            }
            if (string.Equals(stage, "upload", StringComparison.Ordinal)) {
                scenario.BlockedByUser = false;
                scenario.IsPremium = false;
            }
        }
        await scenario.ProcessAsync();

        Assert.Equal(2, scenario.Notices.Count);
        Assert.Equal(new[] { true, false }, scenario.SilentNotices);
        string notice = scenario.Notices[1];
        JsonElement markup = Assert.IsType<JsonElement>(scenario.LastNoticeMarkup);
        JsonElement edit = Assert.Single(markup.GetProperty("inline_keyboard").EnumerateArray().SelectMany(row => row.EnumerateArray()),
            button => string.Equals(button.GetProperty("text").GetString(), russian ? "✏️ Редактировать" : "✏️ Edit", StringComparison.Ordinal));
        Assert.Equal(russian
            ? "✅ Приём пищи сохранён\n🔥 <b>~400 ккал</b>\nБ 10 г · Ж 20 г · У 30 г\n<b>Распознано:</b>\n• Гречка · 150 г · ~300 ккал\n• Курица · 1 шт. · ~100 ккал"
            : "✅ Meal saved to your diary\n🔥 <b>~400 kcal</b>\nP 10 g · F 20 g · C 30 g\n<b>Recognized foods:</b>\n• buckwheat · 150 g · ~300 kcal\n• Chicken · 1 pcs · ~100 kcal",
            notice);
        Assert.Empty(scenario.DeletedMessages);
        if (!rejectProgressNotice) {
            Assert.False(scenario.Completed);
            scenario.ProgressDeletionStatus = HttpStatusCode.InternalServerError;
            await Assert.ThrowsAsync<ApiRequestException>(scenario.ProcessAsync);
            scenario.ProgressDeletionStatus = HttpStatusCode.OK;
            await scenario.ProcessAsync();
            Assert.Equal(new[] { 77, 77 }, scenario.DeletedMessages);
        }
        int billingReads = 1 + (startTrial ? 1 : 0) + (hasConsent ? 0 : 2);
        Assert.Multiple(
            () => Assert.True(scenario.Completed),
            () => Assert.Equal(1, uploads),
            () => Assert.Equal(1, recognitions),
            () => Assert.Equal(1, saves),
            () => Assert.Equal(billingReads,
                scenario.BusinessPaths.Count(path => string.Equals(path, "/api/v1/billing/overview", StringComparison.Ordinal))),
            () => Assert.Equal(startTrial ? 1 : 0, trialStarts),
            () => Assert.Contains(russian ? "Приём пищи сохранён" : "Meal saved to your diary", notice, StringComparison.Ordinal),
            () => Assert.Equal(2, scenario.Notices.Count),
            () => Assert.Contains(russian ? "400 ккал" : "400 kcal", notice, StringComparison.Ordinal),
            () => Assert.Equal($"https://diary.example/meals/{scenario.MealId:D}/edit", edit.GetProperty("web_app").GetProperty("url").GetString()));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ProgressMessageAlreadyGoneOrInaccessible_CompletesWithoutResendingReceipt(HttpStatusCode status) {
        using var scenario = new BotOperationScenario {
            State = new BotPhotoCheckpoint("photo-notified", ProgressMessageId: 77),
            ProgressDeletionStatus = status,
        };

        await scenario.ProcessAsync();

        Assert.True(scenario.Completed);
        Assert.Equal(77, Assert.Single(scenario.DeletedMessages));
        Assert.Empty(scenario.Notices);
        Assert.Empty(scenario.BusinessPaths);
    }

    [Fact]
    public async Task FirstUploadCheckpointConflict_DoesNotSendProcessingNotice() {
        using var scenario = new BotOperationScenario { CheckpointConflict = true };
        scenario.BusinessResponse = _ => Task.FromResult(BotOperationScenario.Json(
            new BotImageUpload("https://storage.example/upload", "food", DateTime.UtcNow.AddMinutes(5), new BotImageAssetId(scenario.ImageId))));

        await scenario.ProcessAsync();

        Assert.Multiple(
            () => Assert.Equal("received", scenario.State.Stage),
            () => Assert.Empty(scenario.Notices));
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
        JsonElement rows = markup.GetProperty("inline_keyboard");
        JsonElement button = Assert.Single(rows[0].EnumerateArray());
        JsonElement premium = Assert.Single(rows[1].EnumerateArray());
        Assert.Multiple(
            () => Assert.True(scenario.Completed),
            () => Assert.Contains("Food Diary Premium", Assert.Single(scenario.Notices), StringComparison.Ordinal),
            () => Assert.Equal(buttonText, button.GetProperty("text").GetString()),
            () => Assert.Equal("https://diary.example/meals/add", button.GetProperty("web_app").GetProperty("url").GetString()),
            () => Assert.Equal("https://diary.example/premium", premium.GetProperty("web_app").GetProperty("url").GetString()),
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
        Assert.Equal(["/api/v1/auth/telegram/bot/auth", "/api/v1/billing/overview", "/api/v1/users/privacy"], scenario.BusinessPaths, StringComparer.Ordinal);
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
