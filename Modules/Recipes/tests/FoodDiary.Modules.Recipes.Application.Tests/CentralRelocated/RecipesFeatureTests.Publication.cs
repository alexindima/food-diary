using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Tests.CentralRelocated;

public partial class RecipesFeatureTests {
    [Fact]
    public async Task Create_PublicRecipeWithGenericPhotoRejectsBeforeSavingOrStagingAsync() {
        var user = User.Create("publication-photo@example.test", "hash");
        var repo = new SingleRecipeRepositoryForCreate();
        var users = new StubUserRepository(user);
        IRecipePublicationQueue queue = Substitute.For<IRecipePublicationQueue>();
        queue.Enabled.Returns(returnThis: true);
        var handler = new CreateRecipeCommandHandler(repo, repo, users,
            AllowImageAssetAccessService.Instance, new AllowAllProductLookupService(), new AllowAllRecipeLookupService(),
            new ImmediateRecipeMutationTransactionRunner(), CreateUserProfileReadService(users), queue);
        var imageId = ImageAssetId.New();
        CreateRecipeCommand command = CreateRecipeCommand(
            user.Id.Value, nameof(Visibility.Public), imageId.Value, steps: [CreateRecipeCreateStep(1, "Cook")]);

        Result<RecipeModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal(RecipeErrors.PublicationImageRequiresPrivateUpload.Code, result.Error.Code),
            () => Assert.Null(repo.LastAddedRecipe));
        await queue.DidNotReceiveWithAnyArgs().StageAsync(default, default, default!, default!, default);
    }

    [Theory]
    [InlineData(nameof(Visibility.Private), 0)]
    [InlineData(nameof(Visibility.Public), 1)]
    public async Task Create_StagesOnlyRequestedPublication(string requestedVisibility, int submissions) {
        var user = User.Create("publication-create@example.test", "hash");
        var repo = new SingleRecipeRepositoryForCreate();
        var users = new StubUserRepository(user);
        var revisionId = Guid.NewGuid();
        IRecipePublicationQueue queue = Substitute.For<IRecipePublicationQueue>();
        queue.Enabled.Returns(returnThis: true);
        queue.StageAsync(Arg.Any<RecipeId>(), user.Id, Arg.Any<string>(), Arg.Any<RecipeModerationContent>(), CancellationToken.None)
            .Returns(new RecipePublicationState(revisionId, "Pending", Reason: null));
        var handler = new CreateRecipeCommandHandler(repo, repo, users,
            AllowImageAssetAccessService.Instance, new AllowAllProductLookupService(), new AllowAllRecipeLookupService(),
            new ImmediateRecipeMutationTransactionRunner(), CreateUserProfileReadService(users), queue);

        RecipeModel result = ResultAssert.Success(await handler.Handle(
            CreateRecipeCommand(user.Id.Value, requestedVisibility, steps: [CreateRecipeCreateStep(1, "Cook")]),
            CancellationToken.None));

        Assert.Equal(nameof(Visibility.Private), result.Visibility);
        Assert.Equal(Visibility.Private, repo.LastAddedRecipe?.Visibility);
        Assert.Equal(submissions == 1 ? revisionId : null, result.PublicationRevisionId);
        await queue.Received(submissions).StageAsync(
            new RecipeId(result.Id), Arg.Is<UserId>(owner => owner == user.Id), Arg.Any<string>(), Arg.Any<RecipeModerationContent>(), CancellationToken.None);
    }
}
