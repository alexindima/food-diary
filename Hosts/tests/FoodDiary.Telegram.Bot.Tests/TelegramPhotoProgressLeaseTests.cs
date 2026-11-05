using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramPhotoProgressLeaseTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompetingWorkerBeforeProgressNotice_PreservesTheCommittedCheckpoint(bool loseLease) {
        using var scenario = new BotOperationScenario();
        scenario.BusinessResponse = _ => Task.FromResult(BotOperationScenario.Json(
            new BotImageUpload("https://storage.example/upload", "food", DateTime.UtcNow.AddMinutes(5), new BotImageAssetId(scenario.ImageId))));
        var advanced = new BotPhotoCheckpoint("image-ready", ImageAssetId: scenario.ImageId, PhotoPremiumApproved: true);
        scenario.BeforeAcquire = attempt => {
            if (attempt == 2) {
                scenario.LeaseConflict = loseLease;
                if (!loseLease) {
                    scenario.State = advanced;
                }
            }
        };

        await scenario.ProcessAsync();

        Assert.Multiple(
            () => Assert.Empty(scenario.Notices),
            () => Assert.False(scenario.Completed),
            () => Assert.Equal(loseLease ? 1 : 2, scenario.Checkpoints),
            () => Assert.Equal(loseLease ? "upload" : "image-ready", scenario.State.Stage));
        if (!loseLease) {
            Assert.Equal(advanced, scenario.State);
        }
    }
}
