using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Tests;

public partial class UpdateRecipeCommandHandlerTests {
    [Fact]
    public async Task PublicUpdate_AppliesOnlyAnApprovedCurrentRevisionAndPreservesPersonalNotes() {
        var owner = UserId.New();
        var recipe = Recipe.Create(owner, "Public name", servings: 2, comment: "Private note");
        recipe.AddStep(1, "Public instructions");
        var revisionId = Guid.NewGuid();
        var claim = new RecipePublicationClaim(revisionId, Guid.NewGuid());
        IRecipePublicationQueue queue = Substitute.For<IRecipePublicationQueue>();
        queue.Enabled.Returns(returnThis: true);
        string? stagedChange = null;
        RecipeModerationContent? content = null;
        queue.StageAsync(recipe.Id, owner, Arg.Any<string>(), Arg.Any<RecipeModerationContent>(), CancellationToken.None)
            .Returns(call => {
                stagedChange = call.Arg<string>();
                content = call.Arg<RecipeModerationContent>();
                return new RecipePublicationState(revisionId, "Pending", Reason: null);
            });
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipe.Id, owner, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("moderation@example.test", "hash")),
            AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService(),
            queue);
        UpdateRecipeCommand change = UpdateCommand(owner.Value, recipe.Id.Value, steps: [
            new RecipeStepInput(1, "Proposed instructions", Title: null, ImageUrl: null, ImageAssetId: null, Ingredients: [
                new RecipeIngredientInput(ProductId: null, NestedRecipeId: null, Amount: 0) { TextName = "Salt", AmountText = "to taste" },
            ]),
        ]) with {
            Name = "Proposed name",
            Comment = "Updated private note",
        };

        RecipeModel ownerView = ResultAssert.Success(await handler.Handle(change, CancellationToken.None));

        Assert.Equal("Public name", recipe.Name);
        Assert.Equal("Public instructions", Assert.Single(recipe.Steps).Instruction);
        Assert.Equal("Updated private note", recipe.Comment);
        Assert.Equal("Pending", ownerView.PublicationStatus);
        Assert.Equal(revisionId, ownerView.PublicationRevisionId);
        Assert.Equal("Proposed name", ownerView.Name);
        Assert.NotNull(content);
        Assert.Contains("Salt", content.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("private note", content.Text, StringComparison.Ordinal);
        Assert.NotNull(stagedChange);
        Assert.DoesNotContain("private note", stagedChange, StringComparison.Ordinal);
        var applier = new RecipePublicationApplier(handler);
        bool canApply = false;
        queue.CanApplyAsync(recipe.Id, claim, CancellationToken.None).Returns(_ => canApply);
        queue.CompleteAsync(recipe.Id, claim, CancellationToken.None).Returns(returnThis: true);

        Result stale = await applier.ApplyAsync(recipe.Id, owner, claim, stagedChange, CancellationToken.None);

        Assert.True(stale.IsFailure);
        Assert.Equal("Public name", recipe.Name);

        queue.GetCurrentAsync(recipe.Id, CancellationToken.None)
            .Returns(new RecipePublicationState(revisionId, "Pending", Reason: null, ChangeJson: stagedChange));
        ResultAssert.Success(await handler.Handle(change with { Comment = "New note saved after submission" }, CancellationToken.None));
        await queue.Received(1).StageAsync(recipe.Id, owner, Arg.Any<string>(), Arg.Any<RecipeModerationContent>(), CancellationToken.None);
        canApply = true;
        Result approved = await applier.ApplyAsync(recipe.Id, owner, claim, stagedChange, CancellationToken.None);

        Assert.True(approved.IsSuccess);
        Assert.Equal("Proposed name", recipe.Name);
        Assert.Equal("Proposed instructions", Assert.Single(recipe.Steps).Instruction);
        Assert.Equal("New note saved after submission", recipe.Comment);
        await queue.Received(1).CompleteAsync(recipe.Id, claim, CancellationToken.None);
    }

    [Fact]
    public async Task PrivateUpdate_SavesWithoutStaging() {
        var owner = UserId.New();
        var recipe = Recipe.Create(owner, "Personal recipe", servings: 2, visibility: Visibility.Private);
        IRecipePublicationQueue queue = Substitute.For<IRecipePublicationQueue>();
        queue.Enabled.Returns(returnThis: true);
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipe.Id, owner, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("private@example.test", "hash")),
            AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService(),
            queue);

        RecipeModel result = ResultAssert.Success(await handler.Handle(
            UpdateCommand(owner.Value, recipe.Id.Value, visibility: nameof(Visibility.Private)), CancellationToken.None));

        Assert.Equal(nameof(Visibility.Private), result.Visibility);
        Assert.Null(result.PublicationStatus);
        await queue.DidNotReceiveWithAnyArgs().StageAsync(default, default, default!, default!, default);
    }

    [Theory]
    [InlineData(Visibility.Private, false)]
    [InlineData(Visibility.Public, true)]
    public async Task PublicSubmission_AllowsGenericPhotoOnlyWhenAlreadyOnPublicRecipe(Visibility originalVisibility, bool allowed) {
        var owner = UserId.New();
        var imageId = new ImageAssetId(Guid.NewGuid());
        var recipe = Recipe.Create(owner, "Existing recipe", servings: 2,
            imageUrl: "https://cdn.example.com/existing.jpg", imageAssetId: imageId, visibility: originalVisibility);
        IRecipePublicationQueue queue = Substitute.For<IRecipePublicationQueue>();
        queue.Enabled.Returns(returnThis: true);
        queue.StageAsync(recipe.Id, owner, Arg.Any<string>(), Arg.Any<RecipeModerationContent>(), CancellationToken.None)
            .Returns(new RecipePublicationState(Guid.NewGuid(), "Pending", Reason: null));
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipe.Id, owner, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("photo-publication@example.test", "hash")),
            AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService(),
            queue);

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(owner.Value, recipe.Id.Value), CancellationToken.None);

        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed) {
            Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        }
        await queue.Received(allowed ? 1 : 0).StageAsync(
            recipe.Id, owner, Arg.Any<string>(), Arg.Any<RecipeModerationContent>(), CancellationToken.None);
    }
}
