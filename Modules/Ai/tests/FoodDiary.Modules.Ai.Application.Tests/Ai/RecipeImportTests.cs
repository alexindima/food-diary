using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Services;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class RecipeImportTests {
    private const string RequestId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private readonly IOpenAiFoodClient _client = Substitute.For<IOpenAiFoodClient>();
    private readonly IRecipeSourceReader _source = Substitute.For<IRecipeSourceReader>();
    private readonly IAiQuotaRepository _quota = Substitute.For<IAiQuotaRepository>();
    private readonly IUserAiProfileReadService _profiles = Substitute.For<IUserAiProfileReadService>();
    private readonly UserId _userId = UserId.New();

    [Fact]
    public async Task Import_WhenConsentMissing_DoesNotLoadSourceOrCallProvider() {
        OpenAiFoodService service = CreateService(consent: false);
        Result<RecipeImportDraftModel> result = await service.ImportRecipeAsync("https://example.org/recipe", text: null, _userId, RequestId, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Ai.ConsentRequired", result.Error.Code);
        Assert.Empty(_source.ReceivedCalls());
        Assert.Empty(_client.ReceivedCalls());
        Assert.Empty(_quota.ReceivedCalls());
    }

    [Fact]
    public async Task Import_WithCaption_SkipsLoadingAndPreservesUnknownQuantities() {
        OpenAiFoodService service = CreateService();
        Result<RecipeImportDraftModel> result = await service.ImportRecipeAsync(
            "https://www.instagram.com/reel/example/?stkn=tracking", "caption", _userId, RequestId, CancellationToken.None);
        RecipeImportDraftModel draft = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal("https://www.instagram.com/reel/example/", draft.SourceUrl),
            () => Assert.Equal("pinch", draft.Ingredients[0].Amount),
            () => Assert.Null(draft.Servings));
        Assert.Empty(_source.ReceivedCalls());
        await _profiles.Received(1).GetAiProfileAsync(_userId, Arg.Any<CancellationToken>());
        await _quota.Received(1).ReconcileAsync(RequestId, Arg.Is<AiQuotaUsage>(x => x.Operation == "recipe-import" && x.TotalTokens == 30), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_WhenSourceFails_DoesNotSpendAiQuota() {
        OpenAiFoodService service = CreateService();
        _source.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<RecipeSource>(AiErrors.RecipeSourceUnavailable()));
        Result<RecipeImportDraftModel> result = await service.ImportRecipeAsync("https://example.org/recipe", text: null, _userId, RequestId, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Empty(_client.ReceivedCalls());
        Assert.Empty(_quota.ReceivedCalls());
    }

    [Fact]
    public async Task Import_WhenQuotaExceeded_DoesNotCallPaidProvider() {
        OpenAiFoodService service = CreateService();
        _quota.ReserveAsync(Arg.Any<AiQuotaReservationRequest>(), Arg.Any<CancellationToken>()).Returns(AiQuotaReservationStatus.QuotaExceeded);
        Result<RecipeImportDraftModel> result = await service.ImportRecipeAsync(sourceUrl: null, "caption", _userId, RequestId, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Ai.QuotaExceeded", result.Error.Code);
        await _client.DidNotReceive().ImportRecipeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_WithNoRecipe_ReconcilesSuccessfulProviderUsage() {
        OpenAiFoodService service = CreateService();
        _client.ImportRecipeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
            Result.Success(new OpenAiFoodClientResponse<RecipeImportDraftModel>(Draft() with { Ingredients = [] }, "recipe-import", "text", new AiUsageTokens(20, 10, 30))));
        Result<RecipeImportDraftModel> result = await service.ImportRecipeAsync(sourceUrl: null, "not a recipe", _userId, RequestId, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Ai.RecipeNotFound", result.Error.Code);
        await _quota.Received(1).ReconcileAsync(RequestId, Arg.Any<AiQuotaUsage>(), Arg.Any<CancellationToken>());
    }

    private OpenAiFoodService CreateService(bool consent = true) {
        _profiles.GetAiProfileAsync(_userId, Arg.Any<CancellationToken>()).Returns(Result.Success(new UserAiProfileModel(_userId, "ru", 10000, 10000, consent)));
        _client.GetRecipeImportTokenBudgetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new AiProviderTokenBudget(100, 100)));
        _client.ImportRecipeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
            Result.Success(new OpenAiFoodClientResponse<RecipeImportDraftModel>(Draft(), "recipe-import", "text", new AiUsageTokens(20, 10, 30))));
        _quota.ReserveAsync(Arg.Any<AiQuotaReservationRequest>(), Arg.Any<CancellationToken>()).Returns(AiQuotaReservationStatus.Acquired);
        return new OpenAiFoodService(_client, _quota, _profiles, TimeProvider.System, Substitute.For<IAiPromptProvider>(), recipeSourceReader: _source);
    }

    private static RecipeImportDraftModel Draft() => new("Salad", Description: null, [new("salt", "pinch")], ["Mix"], Servings: null, PrepMinutes: null, CookMinutes: null, "100 kcal (basis unspecified)", SourceUrl: null);
}
