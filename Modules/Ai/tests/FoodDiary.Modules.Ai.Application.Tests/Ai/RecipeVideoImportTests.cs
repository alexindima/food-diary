using FoodDiary.Modules.Ai.Domain.ValueObjects;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Services;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class RecipeVideoImportTests {
    private const string RequestId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private readonly IOpenAiFoodClient _client = Substitute.For<IOpenAiFoodClient>();
    private readonly IRecipeVideoProcessor _video = Substitute.For<IRecipeVideoProcessor>();
    private readonly IAiQuotaRepository _quota = Substitute.For<IAiQuotaRepository>();
    private readonly IUserAiProfileReadService _profiles = Substitute.For<IUserAiProfileReadService>();
    private readonly UserId _user = UserId.New();

    [Fact]
    public async Task Video_WhenConsentMissing_DoesNotLoadOrProcessMedia() {
        OpenAiFoodService service = CreateService(consent: false);
        ResultAssert.Failure(await service.ImportRecipeVideoAsync(video: null, "https://example.org/video", text: null, _user, RequestId, CancellationToken.None), "Ai.ConsentRequired");
        Assert.Empty(_video.ReceivedCalls());
        Assert.Empty(_client.ReceivedCalls());
        Assert.Empty(_quota.ReceivedCalls());
    }

    [Fact]
    public async Task Video_WhenExtractionFails_DoesNotCallPaidProvider() {
        OpenAiFoodService service = CreateService();
        _video.ExtractAudioAsync(Arg.Any<Stream?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<RecipeAudio>(AiErrors.InvalidRecipeVideo()));
        ResultAssert.Failure(await service.ImportRecipeVideoAsync(video: null, "https://example.org/video", text: null, _user, RequestId, CancellationToken.None));
        Assert.Empty(_quota.ReceivedCalls());
        Assert.Empty(_client.ReceivedCalls());
    }

    [Fact]
    public async Task Video_WithSpeechAndCaption_ReconcilesTwoDistinctCallsAndReadsProfileOnce() {
        OpenAiFoodService service = CreateService();
        RecipeImportDraftModel draft = ResultAssert.Success(await service.ImportRecipeVideoAsync(video: null, "https://example.org/video", "Add a pinch of salt", _user, RequestId, CancellationToken.None));
        Assert.Equal("https://example.org/video", draft.SourceUrl);
        await _profiles.Received(1).GetAiProfileAsync(_user, Arg.Any<CancellationToken>());
        await _client.Received(1).GetRecipeImportTokenBudgetAsync(Arg.Is<string>(x => x.Contains("180g yoghurt", StringComparison.Ordinal) && x.Contains("pinch of salt", StringComparison.Ordinal)), "ru", Arg.Any<CancellationToken>());
        await _quota.Received(1).ReconcileAsync(Arg.Is<string>(x => x != RequestId && x.Length == 64), Arg.Is<AiQuotaUsage>(x => x.Operation == "recipe-transcription" && x.InputTokens == 640 && x.OutputTokens == 640 && x.TotalTokens == 1280), Arg.Any<CancellationToken>());
        await _quota.Received(1).ReconcileAsync(RequestId, Arg.Is<AiQuotaUsage>(x => x.Operation == "recipe-import"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Video_WhenTranscriptionQuotaExceeded_DoesNotTranscribe() {
        OpenAiFoodService service = CreateService();
        _quota.ReserveAsync(Arg.Any<AiQuotaReservationRequest>(), Arg.Any<CancellationToken>()).Returns(AiQuotaReservationStatus.QuotaExceeded);
        ResultAssert.Failure(await service.ImportRecipeVideoAsync(video: null, "https://example.org/video", text: null, _user, RequestId, CancellationToken.None), "Ai.QuotaExceeded");
        Assert.Empty(_client.ReceivedCalls());
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(10)]
    public async Task Video_WithTokenUsage_ReservesBothTokenDirectionsBeforeTranscription(double durationSeconds) {
        OpenAiFoodService service = CreateService();
        _video.ExtractAudioAsync(Arg.Any<Stream?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new RecipeAudio([1, 2], durationSeconds, "https://example.org/video")));
        _client.TranscribeRecipeAudioAsync(Arg.Any<RecipeAudio>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new OpenAiFoodClientResponse<string>("180g yoghurt", "recipe-transcription", "gpt-transcribe", AiTokenUsage.FromCounts(1, 1, 2))));
        _quota.ReserveAsync(Arg.Any<AiQuotaReservationRequest>(), Arg.Any<CancellationToken>()).Returns(call => {
            AiQuotaReservationRequest reservation = call.Arg<AiQuotaReservationRequest>();
            Assert.True(reservation.InputTokens > 0);
            Assert.True(reservation.OutputTokens > 0);
            return AiQuotaReservationStatus.Acquired;
        });

        ResultAssert.Success(await service.ImportRecipeVideoAsync(video: null, "https://example.org/video", text: null, _user, RequestId, CancellationToken.None));

        await _quota.Received(1).ReconcileAsync(
            Arg.Is<string>(x => x != RequestId),
            Arg.Is<AiQuotaUsage>(x => x.Operation == "recipe-transcription" && x.InputTokens == 1 && x.OutputTokens == 1 && x.TotalTokens == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Video_WithInsufficientOutputQuota_DoesNotTranscribe() {
        OpenAiFoodService service = CreateService();
        _profiles.GetAiProfileAsync(_user, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserAiProfileModel(_user, "ru", 10000, 1, HasAcceptedAiConsent: true)));
        _quota.ReserveAsync(Arg.Any<AiQuotaReservationRequest>(), Arg.Any<CancellationToken>()).Returns(call => {
            AiQuotaReservationRequest reservation = call.Arg<AiQuotaReservationRequest>();
            return reservation.InputTokens > reservation.InputTokenLimit || reservation.OutputTokens > reservation.OutputTokenLimit
                ? AiQuotaReservationStatus.QuotaExceeded
                : AiQuotaReservationStatus.Acquired;
        });

        ResultAssert.Failure(await service.ImportRecipeVideoAsync(video: null, "https://example.org/video", text: null, _user, RequestId, CancellationToken.None), "Ai.QuotaExceeded");

        Assert.Empty(_client.ReceivedCalls());
    }

    [Fact]
    public async Task Video_WithNoSpeech_AccountsForTranscriptionAndSkipsRecipeParsing() {
        OpenAiFoodService service = CreateService();
        _client.TranscribeRecipeAudioAsync(Arg.Any<RecipeAudio>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new OpenAiFoodClientResponse<string>("", "recipe-transcription", "gpt-transcribe", Usage: null)));
        ResultAssert.Failure(await service.ImportRecipeVideoAsync(video: null, "https://example.org/video", text: null, _user, RequestId, CancellationToken.None), "Ai.RecipeNotFound");
        await _quota.Received(1).ReconcileAsync(Arg.Any<string>(), Arg.Any<AiQuotaUsage>(), Arg.Any<CancellationToken>());
        await _client.DidNotReceive().GetRecipeImportTokenBudgetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    private OpenAiFoodService CreateService(bool consent = true) {
        _profiles.GetAiProfileAsync(_user, Arg.Any<CancellationToken>()).Returns(Result.Success(new UserAiProfileModel(_user, "ru", 10000, 10000, consent)));
        _video.ExtractAudioAsync(Arg.Any<Stream?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new RecipeAudio([1, 2], 10, "https://example.org/video")));
        _client.TranscribeRecipeAudioAsync(Arg.Any<RecipeAudio>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new OpenAiFoodClientResponse<string>("180g yoghurt", "recipe-transcription", "gpt-transcribe", Usage: null)));
        _client.GetRecipeImportTokenBudgetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new AiProviderTokenBudget(100, 100)));
        _client.ImportRecipeAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new OpenAiFoodClientResponse<RecipeImportDraftModel>(new("Salad", Description: null, [new("yoghurt", "180g")], ["Mix"], Servings: null, PrepMinutes: null, CookMinutes: null, AuthorNutrition: null, SourceUrl: null), "recipe-import", "text", AiTokenUsage.FromCounts(20, 10, 30))));
        _quota.ReserveAsync(Arg.Any<AiQuotaReservationRequest>(), Arg.Any<CancellationToken>()).Returns(AiQuotaReservationStatus.Acquired);
        return new OpenAiFoodService(_client, _quota, _profiles, TimeProvider.System, Substitute.For<IAiPromptProvider>(), recipeVideoProcessor: _video);
    }
}
