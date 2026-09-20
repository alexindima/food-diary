using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Prompts;
using FoodDiary.Modules.Ai.Application.Commands.TestAiPrompt;
using FoodDiary.Modules.Ai.Application.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Application.Queries.GetAiPromptScenarios;
using FoodDiary.Modules.Ai.Contracts.Commands.TestAiPrompt;
using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptScenarios;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class AiPromptWorkbenchTests {
    [Fact]
    public void Catalog_UnknownScenarioHasNoVariables() => Assert.Empty(AiPromptCatalog.GetVariables("unknown"));

    [Fact]
    public async Task Test_InvalidDraft_DoesNotCallProviders() {
        IOpenAiFoodService service = Substitute.For<IOpenAiFoodService>();
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        var draft = new AiPromptDraft("nutrition", "en", "Estimate", Text: null, ImageAssetId: null,
            [new FoodVisionItemModel("apple", NameLocal: null, 0, "g", 1)]);
        ResultAssert.Failure(await new TestAiPromptCommandHandler(service, images)
            .Handle(new TestAiPromptCommand(Guid.NewGuid(), "request", draft), CancellationToken.None));
        Assert.Empty(service.ReceivedCalls());
        Assert.Empty(images.ReceivedCalls());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Test_Nutrition_ForwardsSampleAndPreservesProviderOutcome(bool success) {
        IOpenAiFoodService service = Substitute.For<IOpenAiFoodService>();
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        using var cancellation = new CancellationTokenSource();
        var user = UserId.New();
        FoodVisionItemModel[] items = [new("apple", NameLocal: null, 100, "g", 1)];
        var draft = new AiPromptDraft("nutrition", "ru", "Estimate {{itemsJson}}", Text: null, ImageAssetId: null, items);
        Error error = AiErrors.Forbidden();
        service.CalculateNutritionAsync(items, user, "request", cancellation.Token, new AiPromptOverride(draft.PromptText, "ru"))
            .Returns(success ? Result.Success(new FoodNutritionModel(52, 1, 2, 3, 4, 0, [])) : Result.Failure<FoodNutritionModel>(error));
        Result<string> result = await new TestAiPromptCommandHandler(service, images)
            .Handle(new TestAiPromptCommand(user.Value, "request", draft), cancellation.Token);
        if (success) {
            using var json = System.Text.Json.JsonDocument.Parse(ResultAssert.Success(result));
            Assert.Equal(52, json.RootElement.GetProperty("calories").GetDecimal());
        } else {
            ResultAssert.Failure(result);
            Assert.Equal(error, result.Error);
        }
        await service.Received(1).CalculateNutritionAsync(items, user, "request", cancellation.Token, new AiPromptOverride(draft.PromptText, "ru"));
        Assert.Empty(images.ReceivedCalls());
    }

    [Fact]
    public async Task Test_Photo_UsesAuthorizedImageAndDraft() {
        IOpenAiFoodService service = Substitute.For<IOpenAiFoodService>();
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        using var cancellation = new CancellationTokenSource();
        var user = UserId.New();
        var image = ImageAssetId.New();
        var draft = new AiPromptDraft("vision", "en", "Find foods", "plate", image.Value, Items: null);
        images.GetDataUrlAsync(image, user, cancellation.Token).Returns(Result.Success("data:image/png;base64,sample"));
        service.AnalyzeFoodImageAsync("data:image/png;base64,sample", user, "plate", "request", cancellation.Token,
            new AiPromptOverride("Find foods", "en")).Returns(Result.Success(new FoodVisionModel([])));
        string result = ResultAssert.Success(await new TestAiPromptCommandHandler(service, images)
            .Handle(new TestAiPromptCommand(user.Value, "request", draft), cancellation.Token));
        Assert.Contains("items", result, StringComparison.Ordinal);
        await service.Received(1).AnalyzeFoodImageAsync("data:image/png;base64,sample", user, "plate", "request", cancellation.Token,
            new AiPromptOverride("Find foods", "en"));
    }

    [Fact]
    public async Task Catalog_EmptyDatabase_ExposesAllSupportedScenariosAndLanguages() {
        IAiPromptTemplateReadModelRepository repository = Substitute.For<IAiPromptTemplateReadModelRepository>();
        repository.GetAllReadModelsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<AiPromptTemplateReadModel>());
        var handler = new GetAiPromptScenariosQueryHandler(repository, Substitute.For<IAiPromptPreviewRenderer>());
        IReadOnlyList<AiPromptScenarioModel> scenarios = await handler.Handle(new GetAiPromptScenariosQuery(), CancellationToken.None);
        Assert.Equal(6, scenarios.Count);
        Assert.All(scenarios, item => {
            Assert.Equal("built-in", item.Source);
            Assert.Equal(AiPromptCatalog.GetDefault(item.Key), item.PromptText);
            Assert.Null(item.Template);
        });
    }

    [Fact]
    public async Task Catalog_InactiveRussianOverride_ExplainsEnglishFallbackAndKeepsDraft() {
        IAiPromptTemplateReadModelRepository repository = Substitute.For<IAiPromptTemplateReadModelRepository>();
        repository.GetAllReadModelsAsync(Arg.Any<CancellationToken>()).Returns(new[] {
            Template("en", "English", active: true), Template("ru", "Russian draft", active: false),
        });
        IReadOnlyList<AiPromptScenarioModel> result = await new GetAiPromptScenariosQueryHandler(repository, Substitute.For<IAiPromptPreviewRenderer>())
            .Handle(new GetAiPromptScenariosQuery(), CancellationToken.None);
        AiPromptScenarioModel russian = Assert.Single(result, item => string.Equals(item.Key, "vision", StringComparison.Ordinal) && string.Equals(item.Locale, "ru", StringComparison.Ordinal));
        Assert.Multiple(() => Assert.Equal("English", russian.PromptText), () => Assert.Equal("english", russian.Source),
            () => Assert.Equal("Russian draft", russian.Template!.PromptText), () => Assert.Equal("English", russian.InheritedPromptText));
        AiPromptScenarioModel english = Assert.Single(result, item => string.Equals(item.Key, "vision", StringComparison.Ordinal) && string.Equals(item.Locale, "en", StringComparison.Ordinal));
        Assert.Equal("built-in", english.InheritedSource);
    }

    [Theory]
    [InlineData("text-parse", "Ignore the input", false)]
    [InlineData("text-parse", "Parse {{userText}} {{languageHint}}", true)]
    [InlineData("vision", "Find {{userText}}", false)]
    [InlineData("nutrition", "Estimate {{itemsJson}}", true)]
    [InlineData("unknown", "Anything", false)]
    [InlineData("NUTRITION", "Estimate food", false)]
    public void Validation_RejectsUnknownScenariosAndUnsupportedOrMissingVariables(string key, string text, bool valid) =>
        Assert.Equal(valid, AiPromptCatalog.IsValid(key, text));

    [Fact]
    public async Task Apply_InvalidTextTemplate_DoesNotWrite() {
        IAiPromptTemplateWriteRepository repository = Substitute.For<IAiPromptTemplateWriteRepository>();
        var handler = new UpsertAiPromptCommandHandler(repository);
        ResultAssert.Failure(await handler.Handle(new UpsertAiPromptCommand("text-parse", "en", "Missing input", IsActive: true), CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task Test_Text_UsesUnsavedDraftAndLocaleWithoutAccessingPromptStorage() {
        IOpenAiFoodService service = Substitute.For<IOpenAiFoodService>();
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        var user = UserId.New();
        var draft = new AiPromptDraft("text-parse", "ru", "Parse {{userText}}", "apple", ImageAssetId: null, Items: null);
        service.ParseFoodTextAsync("apple", user, "request", Arg.Any<CancellationToken>(), new AiPromptOverride(draft.PromptText, "ru"))
            .Returns(Result.Success(new FoodVisionModel([])));
        var handler = new TestAiPromptCommandHandler(service, images);
        string result = ResultAssert.Success(await handler.Handle(new TestAiPromptCommand(user.Value, "request", draft), CancellationToken.None));
        Assert.Contains("items", result, StringComparison.Ordinal);
        Assert.Empty(images.ReceivedCalls());
    }

    [Fact]
    public async Task Test_Photo_DeniedImageNeverReachesAi() {
        IOpenAiFoodService service = Substitute.For<IOpenAiFoodService>();
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        var user = UserId.New();
        var image = ImageAssetId.New();
        images.GetDataUrlAsync(image, user, Arg.Any<CancellationToken>()).Returns(Result.Failure<string>(AiErrors.Forbidden()));
        var draft = new AiPromptDraft("vision", "en", "Find foods", Text: null, image.Value, Items: null);
        var handler = new TestAiPromptCommandHandler(service, images);
        ResultAssert.Failure(await handler.Handle(new TestAiPromptCommand(user.Value, "request", draft), CancellationToken.None));
        Assert.Empty(service.ReceivedCalls());
        await images.Received(1).GetDataUrlAsync(image, user, Arg.Any<CancellationToken>());
    }

    private static AiPromptTemplateReadModel Template(string locale, string text, bool active) =>
        new(Guid.NewGuid(), "vision", locale, text, 1, active, DateTime.UtcNow, ModifiedOnUtc: null);
}
