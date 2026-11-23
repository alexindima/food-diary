using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
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
    [Theory]
    [InlineData(false, 1, true, null)]
    [InlineData(false, 1, false, "no longer current")]
    [InlineData(true, 1, true, "already used")]
    [InlineData(true, 0, false, "no longer current")]
    public async Task Approval_RespectsRecipeUseAndRequiresClaimCompletionAsync(
        bool changedWhilePending, int usageCount, bool completed, string? expectedError) {
        var owner = UserId.New();
        var recipe = Recipe.Create(owner, "Pending recipe", servings: 2, visibility: Visibility.Private);
        recipe.AddStep(1, "Cook");
        string submitted = RecipePublicationDraft.Capture(recipe).Serialize();
        var claim = new RecipePublicationClaim(Guid.NewGuid(), Guid.NewGuid());
        IRecipePublicationQueue queue = Substitute.For<IRecipePublicationQueue>();
        queue.CanApplyAsync(recipe.Id, claim, CancellationToken.None).Returns(returnThis: true);
        queue.CompleteAsync(recipe.Id, claim, CancellationToken.None).Returns(completed);
        IRecipeRepository repository = CreateRecipeRepository(recipe.Id, owner, recipe);
        repository.GetUsageCountAsync(recipe.Id, owner, includePublic: false, CancellationToken.None).Returns(usageCount);
        if (changedWhilePending) {
            recipe.AddStep(2, "Cool");
        }
        var applier = new RecipePublicationApplier(UpdateRecipeHandler(
            repository, new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("approval@example.test", "hash")), AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(), new AllowAllRecipeLookupService(), queue));

        Result result = await applier.ApplyAsync(recipe.Id, owner, claim, submitted, CancellationToken.None);

        if (expectedError is null) {
            ResultAssert.Success(result);
            Assert.Equal(Visibility.Public, recipe.Visibility);
            Assert.Equal("Cook", Assert.Single(recipe.Steps).Instruction);
        } else {
            ResultAssert.Failure(result);
            Assert.Contains(expectedError, result.Error.Message, StringComparison.Ordinal);
            if (changedWhilePending && usageCount > 0) {
                Assert.Equal(Visibility.Private, recipe.Visibility);
                Assert.Equal(2, recipe.Steps.Count);
            }
        }
        await queue.Received(changedWhilePending && usageCount > 0 ? 0 : 1).CompleteAsync(recipe.Id, claim, CancellationToken.None);
    }

    [Fact]
    public async Task Approval_RejectsDraftForAnotherOwnerBeforeLoadingRecipeAsync() {
        var owner = UserId.New();
        var recipe = Recipe.Create(owner, "Pending recipe", servings: 2);
        var claim = new RecipePublicationClaim(Guid.NewGuid(), Guid.NewGuid());
        IRecipeRepository repository = Substitute.For<IRecipeRepository>();
        var applier = new RecipePublicationApplier(UpdateRecipeHandler(
            repository, new NoopImageAssetCleanupService(), CreateUserRepository(User.Create("owner@example.test", "hash")),
            AllowImageAssetAccessService.Instance, new AllowAllProductLookupService(), new AllowAllRecipeLookupService()));

        Result result = await applier.ApplyAsync(
            recipe.Id, UserId.New(), claim, RecipePublicationDraft.Capture(recipe).Serialize(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("The proposed recipe does not match the publication.", result.Error.Message);
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task PublicUpdate_WithInvalidNutritionDoesNotChangeOrStageRecipeAsync() {
        var owner = UserId.New();
        var recipe = Recipe.Create(owner, "Current recipe", servings: 2, visibility: Visibility.Public);
        IRecipePublicationQueue queue = Substitute.For<IRecipePublicationQueue>();
        queue.Enabled.Returns(returnThis: true);
        IRecipeRepository repository = CreateRecipeRepository(recipe.Id, owner, recipe);
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            repository, new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("invalid-nutrition@example.test", "hash")), AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(), new AllowAllRecipeLookupService(), queue);

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(owner.Value, recipe.Id.Value, calculateNutritionAutomatically: false), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal(ErrorKind.Validation, result.Error.Kind),
            () => Assert.Equal("Current recipe", recipe.Name));
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
        await queue.DidNotReceiveWithAnyArgs().StageAsync(default, default, default!, default!, default);
    }

    [Fact]
    public async Task PublicUpdate_WhenPhotoAccessFailsDuringAdmissionDoesNotStageRecipeAsync() {
        var owner = UserId.New();
        var recipe = Recipe.Create(owner, "Current recipe", servings: 2, visibility: Visibility.Private);
        var imageId = ImageAssetId.New();
        var photo = new ImageAssetReadModel(imageId, "https://cdn.example.com/recipe.jpg", IsPrivate: true);
        Error accessError = Errors.Validation.Invalid("ImageAssetId", "Photo is no longer available.");
        IImageAssetAccessService imageAccess = Substitute.For<IImageAssetAccessService>();
        imageAccess.ResolveOptionalAsync(assetId: null, owner, CancellationToken.None)
            .Returns(Result.Success<ImageAssetReadModel?>(value: null));
        imageAccess.ResolveOptionalAsync(imageId, owner, CancellationToken.None)
            .Returns(Result.Success<ImageAssetReadModel?>(photo), Result.Failure<ImageAssetReadModel?>(accessError));
        IRecipePublicationQueue queue = Substitute.For<IRecipePublicationQueue>();
        queue.Enabled.Returns(returnThis: true);
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipe.Id, owner, recipe), new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("photo-race@example.test", "hash")), imageAccess,
            new AllowAllProductLookupService(), new AllowAllRecipeLookupService(), queue);

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(owner.Value, recipe.Id.Value, imageAssetId: imageId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal(accessError, result.Error),
            () => Assert.Equal(Visibility.Private, recipe.Visibility),
            () => Assert.Null(recipe.ImageAssetId));
        await queue.DidNotReceiveWithAnyArgs().StageAsync(default, default, default!, default!, default);
    }

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
