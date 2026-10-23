using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Mediator;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;
using FoodDiary.Modules.Recipes.Application.Commands.ImportCatalogRecipe;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Domain.Primitives;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using ResultAssert = FoodDiary.Testing.Assertions.ResultAssert;

namespace FoodDiary.Modules.Recipes.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CatalogRecipeImportTests {
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(12)]
    public async Task Import_PreservesOptionalPreparationTime(int? prepTime) {
        CatalogRecipeModel item = ValidRecipe() with { PrepTime = prepTime };
        var ownerId = Guid.NewGuid();
        ISender sender = Substitute.For<ISender>();
        CreateRecipeCommand? captured = null;
        Recipe? created = null;
        var values = new CreateRecipeValues(new UserId(ownerId), Visibility.Public, ImageAssetId: null, ImageAsset: null);
        sender.Send(Arg.Any<CreateRecipeCommand>(), Arg.Any<CancellationToken>()).Returns(call => {
            captured = call.Arg<CreateRecipeCommand>();
            created = RecipeCreateFactory.Create(captured, values);
            return Result.Success(created.ToModel(usageCount: 0, isOwnedByCurrentUser: true));
        });
        var handler = new ImportCatalogRecipeCommandHandler(Substitute.For<IRecipeCatalogIdReadService>(), new CreateRecipeCommandValidator(), sender);

        CatalogRecipeImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogRecipeCommand(ownerId, item, Preview: false), CancellationToken.None));

        Assert.Equal("imported", result.Status);
        Assert.NotNull(created);
        Assert.Equal(prepTime, created.PrepTime);
        Assert.NotNull(captured);
        Recipe ordinary = RecipeCreateFactory.Create(captured with { CatalogImportId = null }, values);
        Assert.Equal(prepTime, ordinary.PrepTime);
    }

    [Fact]
    public async Task Import_PreservesIdLanguageAndManualNutrition() {
        CatalogRecipeModel item = ValidRecipe() with {
            CalculateNutritionAutomatically = false,
            ManualCalories = 120,
            ManualProteins = 4,
            ManualFats = 2,
            ManualCarbs = 22,
            ManualFiber = 1,
            ManualAlcohol = 0,
        };
        var ownerId = Guid.NewGuid();
        ISender sender = Substitute.For<ISender>();
        CreateRecipeCommand? captured = null;
        sender.Send(Arg.Any<CreateRecipeCommand>(), Arg.Any<CancellationToken>()).Returns(call => {
            captured = call.Arg<CreateRecipeCommand>();
            Recipe recipe = RecipeCreateFactory.Create(captured, new CreateRecipeValues(new UserId(ownerId), Visibility.Public, ImageAssetId: null, ImageAsset: null));
            return Result.Success(recipe.ToModel(usageCount: 0, isOwnedByCurrentUser: true));
        });
        var handler = new ImportCatalogRecipeCommandHandler(Substitute.For<IRecipeCatalogIdReadService>(), new CreateRecipeCommandValidator(), sender);

        CatalogRecipeImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogRecipeCommand(ownerId, item, Preview: false), CancellationToken.None));

        Assert.NotNull(captured);
        Assert.Multiple(() => Assert.Equal("imported", result.Status), () => Assert.Equal(item.Id, captured.CatalogImportId),
            () => Assert.Equal(ownerId, captured.UserId), () => Assert.Equal("ru", captured.Language),
            () => Assert.Equal(120d, captured.ManualCalories), () => Assert.False(captured.CalculateNutritionAutomatically));
    }

    [Theory]
    [InlineData(null, "ready")]
    [InlineData(true, "skipped")]
    [InlineData(false, "invalid")]
    public async Task Preview_DistinguishesNewPublicAndPrivateIds(bool? isPublic, string expectedStatus) {
        CatalogRecipeModel item = ValidRecipe();
        IRecipeCatalogIdReadService repository = Substitute.For<IRecipeCatalogIdReadService>();
        repository.CatalogIdIsPublicAsync(item.Id, Arg.Any<CancellationToken>()).Returns(isPublic);
        ISender sender = Substitute.For<ISender>();
        var handler = new ImportCatalogRecipeCommandHandler(repository, new CreateRecipeCommandValidator(), sender);

        CatalogRecipeImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogRecipeCommand(Guid.NewGuid(), item, Preview: true), CancellationToken.None));

        Assert.Equal(expectedStatus, result.Status);
        Assert.Empty(sender.ReceivedCalls());
    }

    [Fact]
    public async Task Preview_RejectsEnvironmentSpecificImageAssets() {
        CatalogRecipeModel item = ValidRecipe();
        item = item with { Steps = [item.Steps[0] with { ImageAssetId = Guid.NewGuid() }] };
        ISender sender = Substitute.For<ISender>();
        var handler = new ImportCatalogRecipeCommandHandler(Substitute.For<IRecipeCatalogIdReadService>(), new CreateRecipeCommandValidator(), sender);

        CatalogRecipeImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogRecipeCommand(Guid.NewGuid(), item, Preview: true), CancellationToken.None));

        Assert.Equal("invalid", result.Status);
        Assert.Empty(sender.ReceivedCalls());
    }

    [Fact]
    public async Task Preview_RejectsMalformedSteps() {
        var handler = new ImportCatalogRecipeCommandHandler(Substitute.For<IRecipeCatalogIdReadService>(), new CreateRecipeCommandValidator(), Substitute.For<ISender>());
        CatalogRecipeImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogRecipeCommand(Guid.NewGuid(), ValidRecipe() with { Steps = [null!] }, Preview: true), CancellationToken.None));
        Assert.Equal("invalid", result.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyCatalogIdentity_IsRejectedBeforeLookupOrCreation(bool preview) {
        IRecipeCatalogIdReadService repository = Substitute.For<IRecipeCatalogIdReadService>();
        ISender sender = Substitute.For<ISender>();
        var handler = new ImportCatalogRecipeCommandHandler(repository, new CreateRecipeCommandValidator(), sender);

        CatalogRecipeImportResult result = ResultAssert.Success(await handler.Handle(
            new ImportCatalogRecipeCommand(Guid.NewGuid(), ValidRecipe() with { Id = Guid.Empty }, preview), CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(Guid.Empty, result.Id),
            () => Assert.Equal("invalid", result.Status),
            () => Assert.Equal(new[] { "Id must not be empty." }, result.Errors),
            () => Assert.Empty(repository.ReceivedCalls()),
            () => Assert.Empty(sender.ReceivedCalls()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidCreateValues_ReportFieldErrorsAndNeverDispatchCreation(bool preview) {
        CatalogRecipeModel item = ValidRecipe() with { Name = string.Empty, Servings = 0 };
        using var cancellation = new CancellationTokenSource();
        IRecipeCatalogIdReadService repository = Substitute.For<IRecipeCatalogIdReadService>();
        ISender sender = Substitute.For<ISender>();
        var handler = new ImportCatalogRecipeCommandHandler(repository, new CreateRecipeCommandValidator(), sender);

        CatalogRecipeImportResult result = ResultAssert.Success(await handler.Handle(
            new ImportCatalogRecipeCommand(Guid.NewGuid(), item, preview), cancellation.Token));

        Assert.Multiple(
            () => Assert.Equal(item.Id, result.Id),
            () => Assert.Equal("invalid", result.Status),
            () => Assert.Equal(new[] { "Name: Name is required", "Servings: Servings must be greater than zero" }, result.Errors),
            () => Assert.Empty(sender.ReceivedCalls()));
        await repository.Received(1).CatalogIdIsPublicAsync(item.Id, cancellation.Token);
    }

    [Fact]
    public async Task FailedCreation_ReturnsTheOwnerErrorWithoutReportingAnImportOrRetrying() {
        CatalogRecipeModel item = ValidRecipe();
        var ownerId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var error = new Error("Recipes.CreationRejected", "The recipe cannot be created.");
        IRecipeCatalogIdReadService repository = Substitute.For<IRecipeCatalogIdReadService>();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<CreateRecipeCommand>(), cancellation.Token).Returns(Result.Failure<RecipeModel>(error));
        var handler = new ImportCatalogRecipeCommandHandler(repository, new CreateRecipeCommandValidator(), sender);

        Error result = ResultAssert.Failure(await handler.Handle(
            new ImportCatalogRecipeCommand(ownerId, item, Preview: false), cancellation.Token));

        Assert.Same(error, result);
        await sender.Received(1).Send(Arg.Is<CreateRecipeCommand>(command => command.CatalogImportId == item.Id && command.UserId == ownerId), cancellation.Token);
        Assert.Single(sender.ReceivedCalls());
        await repository.Received(1).CatalogIdIsPublicAsync(item.Id, cancellation.Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingIngredientCollectionsOrEntries_ReturnAnInvalidReportBeforeCreation(bool missingCollection) {
        CatalogRecipeModel item = ValidRecipe();
        item = item with { Steps = [item.Steps[0] with { Ingredients = missingCollection ? null! : [null!] }] };
        ISender sender = Substitute.For<ISender>();
        var handler = new ImportCatalogRecipeCommandHandler(Substitute.For<IRecipeCatalogIdReadService>(), new CreateRecipeCommandValidator(), sender);

        CatalogRecipeImportResult result = ResultAssert.Success(await handler.Handle(
            new ImportCatalogRecipeCommand(Guid.NewGuid(), item, Preview: false), CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(item.Id, result.Id),
            () => Assert.Equal("invalid", result.Status),
            () => Assert.Equal(new[] { "Steps and ingredients must not be null." }, result.Errors),
            () => Assert.Empty(sender.ReceivedCalls()));
    }

    private static CatalogRecipeModel ValidRecipe() => new(
        Guid.NewGuid(), "Каша", Description: null, "breakfast", ImageUrl: null, PrepTime: null, CookTime: null,
        Servings: 1, "ru", LanguageConfirmed: true, CalculateNutritionAutomatically: true,
        ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
        [new RecipeStepInput(Order: 1, "Варить до готовности", Title: null, ImageUrl: null, ImageAssetId: null, [])]);
}
