using System.Text.Json;
using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramPhotoTrialTests {
    [Theory]
    [InlineData("en", "send the photo again")]
    [InlineData("ru", "отправьте фото снова")]
    public async Task ExpiredAcceptedTrialWithMissingConsentClosesTheOfferWithoutStartingATrial(string language, string expectedMessage) {
        using var scenario = new BotOperationScenario {
            IsPremium = false,
            CanStartTrial = true,
            HasAiConsent = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-16),
            State = new BotPhotoCheckpoint(BotPhotoTrial.Accepted),
        };
        scenario.Incoming = scenario.Incoming with { Language = language };

        await scenario.ProcessAsync();

        Assert.True(scenario.Completed);
        Assert.Contains(expectedMessage, Assert.Single(scenario.Notices), StringComparison.Ordinal);
        Assert.DoesNotContain(scenario.BusinessPaths, path => path.EndsWith("/trial", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AcceptedTrialWhoseEligibilityWasLostCannotProceedToUpload() {
        using var scenario = new BotOperationScenario {
            IsPremium = false,
            CanStartTrial = false,
            State = new BotPhotoCheckpoint(BotPhotoTrial.Accepted),
        };

        await scenario.ProcessAsync();

        Assert.Equal("failed", scenario.State.Stage);
        Assert.Equal(BotRecognitionFailure.PremiumRequired, scenario.State.ErrorCode);
        Assert.DoesNotContain(scenario.BusinessPaths, path => path.EndsWith("/trial", StringComparison.Ordinal) || path.Contains("upload", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task TrialAcceptanceWithoutRequiredConsents_PreservesPhotoAndDoesNotConsumeTheTrial(bool hasAiConsent, bool hasHealthConsent) {
        using var scenario = new BotOperationScenario {
            IsPremium = false,
            CanStartTrial = true,
            HasAiConsent = hasAiConsent,
            HasHealthConsent = hasHealthConsent,
            State = new BotPhotoCheckpoint(BotPhotoTrial.Accepted),
        };
        scenario.Incoming = scenario.Incoming with { Language = "ru" };

        await scenario.ProcessAsync();

        Assert.Equal(BotPhotoTrial.Offered, scenario.State.Stage);
        Assert.False(scenario.Completed);
        Assert.Equal(scenario.CreatedAtUtc.AddMinutes(15), scenario.NextAttemptAtUtc);
        Assert.Equal(["/api/v1/auth/telegram/bot/auth", "/api/v1/users/privacy"], scenario.BusinessPaths, StringComparer.Ordinal);
        Assert.Contains("согласия на обработку фото с помощью ИИ", Assert.Single(scenario.Notices), StringComparison.Ordinal);
        JsonElement markup = Assert.IsType<JsonElement>(scenario.LastNoticeMarkup);
        JsonElement button = Assert.Single(markup.GetProperty("inline_keyboard")[0].EnumerateArray());
        Assert.Equal("https://diary.example/privacy-settings", button.GetProperty("web_app").GetProperty("url").GetString());
        Assert.True(await scenario.AcceptTrialAsync());
    }

    [Fact]
    public async Task ExpiredOffer_ClearsTheOperationWithoutDownloadingThePhoto() {
        using var scenario = new BotOperationScenario {
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-16),
            State = new BotPhotoCheckpoint(BotPhotoTrial.Offered),
        };

        await scenario.ProcessAsync();

        Assert.True(scenario.Completed);
        Assert.Empty(scenario.BusinessPaths);
        Assert.Empty(scenario.Notices);
        Assert.False(await scenario.AcceptTrialAsync());
    }
}
