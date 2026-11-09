using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramPhotoConsentTests {
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task MissingConsent_StopsAnExistingUploadBeforeDownloadingThePhoto(bool hasAiConsent, bool hasHealthConsent) {
        using var scenario = new BotOperationScenario { HasAiConsent = hasAiConsent, HasHealthConsent = hasHealthConsent };
        var upload = new BotImageUpload("https://storage.example/upload", "food", DateTime.UtcNow.AddMinutes(5), new BotImageAssetId(scenario.ImageId));
        scenario.State = new BotPhotoCheckpoint("upload", Upload: upload, ImageAssetId: scenario.ImageId,
            PhotoPremiumApproved: true, ProgressMessageId: 77);

        await scenario.ProcessAsync();
        await scenario.ProcessAsync();

        Assert.Equal(BotPhotoCallback.WaitingForConsent, scenario.State.Stage);
        Assert.False(scenario.Completed);
        Assert.Equal(["/api/v1/auth/telegram/bot/auth", "/api/v1/users/privacy"], scenario.BusinessPaths, StringComparer.Ordinal);
        Assert.Equal(77, Assert.Single(scenario.DeletedMessages));
        Assert.Contains("Continue recognition", Assert.Single(scenario.Notices), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExpiredContinuation_RejectsTheButtonAndClearsPendingWork() {
        using var scenario = new BotOperationScenario { HasHealthConsent = false };
        await scenario.ProcessAsync();
        await scenario.ProcessAsync();
        JsonElement markup = Assert.IsType<JsonElement>(scenario.LastNoticeMarkup);
        string? callback = markup.GetProperty("inline_keyboard")[1][0].GetProperty("callback_data").GetString();
        scenario.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-16);
        scenario.HasHealthConsent = true;

        Assert.False(await scenario.ResumePhotoAsync(callback));
        await scenario.ProcessAsync();

        Assert.True(scenario.Completed);
        Assert.Single(scenario.Notices);
        Assert.DoesNotContain(scenario.BusinessPaths, path => path.EndsWith("/getFile", StringComparison.OrdinalIgnoreCase));
    }
}
