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
            FoodDiary.Modules.Recipes.Domain.Entities.Recipe recipe = RecipeCreateFactory.Create(captured, new CreateRecipeValues(new UserId(ownerId), Visibility.Public, ImageAssetId: null, ImageAsset: null));
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

    private static CatalogRecipeModel ValidRecipe() => new(
        Guid.NewGuid(), "Каша", Description: null, "breakfast", ImageUrl: null, PrepTime: null, CookTime: null,
        Servings: 1, "ru", LanguageConfirmed: true, CalculateNutritionAutomatically: true,
        ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
        [new RecipeStepInput(Order: 1, "Варить до готовности", Title: null, ImageUrl: null, ImageAssetId: null, [])]);
}
