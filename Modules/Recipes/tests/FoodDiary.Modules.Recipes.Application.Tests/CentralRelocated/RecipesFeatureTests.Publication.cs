using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Tests.CentralRelocated;

public partial class RecipesFeatureTests {
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
