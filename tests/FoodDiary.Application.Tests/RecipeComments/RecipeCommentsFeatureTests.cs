using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.UpdateRecipeComment;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Queries.GetRecipeComments;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Recipes;

using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Tests.RecipeComments;

[ExcludeFromCodeCoverage]
public class RecipeCommentsFeatureTests {
    [Fact]
    public async Task CreateRecipeComment_WithValidData_Succeeds() {
        var userId = UserId.New();
        var ownerId = UserId.New();
        var recipe = Recipe.Create(ownerId, "Pasta", 1);
        var commentRepo = new InMemoryRecipeCommentRepository();
        INotificationRepository notificationRepository = CreateNotificationRepository(out List<Notification> addedNotifications);

        var handler = new CreateRecipeCommentCommandHandler(
            commentRepo,
            CreateRecipeRepository(recipe),
            notificationRepository);
        Result<RecipeCommentModel> result = await handler.Handle(
            new CreateRecipeCommentCommand(userId.Value, recipe.Id.Value, "Delicious!"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Delicious!", result.Value.Text);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Single(addedNotifications); // notification for recipe owner
    }

    [Fact]
    public async Task CreateRecipeComment_OnOwnRecipe_DoesNotCreateNotification() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Pasta", 1);
        INotificationRepository notificationRepository = CreateNotificationRepository(out List<Notification> addedNotifications);

        var handler = new CreateRecipeCommentCommandHandler(
            new InMemoryRecipeCommentRepository(),
            CreateRecipeRepository(recipe),
            notificationRepository);
        Result<RecipeCommentModel> result = await handler.Handle(
            new CreateRecipeCommentCommand(userId.Value, recipe.Id.Value, "My note"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(addedNotifications);
    }

    [Fact]
    public async Task CreateRecipeComment_WhenRecipeNotFound_ReturnsFailure() {
        var handler = new CreateRecipeCommentCommandHandler(
            new InMemoryRecipeCommentRepository(),
            CreateRecipeRepository(recipe: null),
            CreateNotificationRepository());

        Result<RecipeCommentModel> result = await handler.Handle(
            new CreateRecipeCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "Text"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task CreateRecipeComment_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new CreateRecipeCommentCommandHandler(
            new InMemoryRecipeCommentRepository(),
            CreateRecipeRepository(Recipe.Create(UserId.New(), "Pasta", 1)),
            CreateNotificationRepository());

        Result<RecipeCommentModel> result = await handler.Handle(
            new CreateRecipeCommentCommand(Guid.Empty, Guid.NewGuid(), "Text"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateRecipeComment_ByAuthor_Succeeds() {
        var userId = UserId.New();
        var comment = RecipeComment.Create(userId, RecipeId.New(), "Old text");
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(comment);

        var handler = new UpdateRecipeCommentCommandHandler(repo);
        Result<RecipeCommentModel> result = await handler.Handle(
            new UpdateRecipeCommentCommand(userId.Value, comment.Id.Value, "New text"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("New text", result.Value.Text);
    }

    [Fact]
    public async Task UpdateRecipeComment_ByNonAuthor_ReturnsFailure() {
        var authorId = UserId.New();
        var otherUserId = UserId.New();
        var comment = RecipeComment.Create(authorId, RecipeId.New(), "Text");
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(comment);

        var handler = new UpdateRecipeCommentCommandHandler(repo);
        Result<RecipeCommentModel> result = await handler.Handle(
            new UpdateRecipeCommentCommand(otherUserId.Value, comment.Id.Value, "Hacked"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NotAuthor", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateRecipeComment_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new UpdateRecipeCommentCommandHandler(new InMemoryRecipeCommentRepository());

        Result<RecipeCommentModel> result = await handler.Handle(
            new UpdateRecipeCommentCommand(Guid.Empty, Guid.NewGuid(), "Text"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateRecipeComment_WhenCommentMissing_ReturnsNotFound() {
        var handler = new UpdateRecipeCommentCommandHandler(new InMemoryRecipeCommentRepository());

        Result<RecipeCommentModel> result = await handler.Handle(
            new UpdateRecipeCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "Text"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("RecipeComment.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRecipeComment_ByAuthor_Succeeds() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var comment = RecipeComment.Create(userId, recipeId, "Text");
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(comment);

        var handler = new DeleteRecipeCommentCommandHandler(repo, CreateRecipeRepository(recipe: null));
        Result result = await handler.Handle(
            new DeleteRecipeCommentCommand(userId.Value, recipeId.Value, comment.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
    }

    [Fact]
    public async Task DeleteRecipeComment_CommentNotFound_ReturnsFailure() {
        var handler = new DeleteRecipeCommentCommandHandler(
            new InMemoryRecipeCommentRepository(), CreateRecipeRepository(recipe: null));

        Result result = await handler.Handle(
            new DeleteRecipeCommentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task DeleteRecipeComment_ByRecipeOwnerWithCommentFromDifferentRecipe_ReturnsNotFound() {
        var ownerId = UserId.New();
        var ownedRecipe = Recipe.Create(ownerId, "Owned", 1);
        var otherRecipeId = RecipeId.New();
        var comment = RecipeComment.Create(UserId.New(), otherRecipeId, "Text");
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(comment);

        var handler = new DeleteRecipeCommentCommandHandler(repo, CreateRecipeRepository(ownedRecipe));
        Result result = await handler.Handle(
            new DeleteRecipeCommentCommand(ownerId.Value, ownedRecipe.Id.Value, comment.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("RecipeComment.NotFound", result.Error.Code);
        Assert.NotNull(await repo.GetByIdAsync(comment.Id));
    }

    [Fact]
    public async Task DeleteRecipeComment_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new DeleteRecipeCommentCommandHandler(
            new InMemoryRecipeCommentRepository(),
            CreateRecipeRepository(recipe: null));

        Result result = await handler.Handle(
            new DeleteRecipeCommentCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRecipeComment_ByNonAuthorWithoutRecipeAccess_ReturnsNotAuthor() {
        var recipeId = RecipeId.New();
        var comment = RecipeComment.Create(UserId.New(), recipeId, "Text");
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(comment);

        var handler = new DeleteRecipeCommentCommandHandler(repo, CreateRecipeRepository(recipe: null));
        Result result = await handler.Handle(
            new DeleteRecipeCommentCommand(Guid.NewGuid(), recipeId.Value, comment.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("RecipeComment.NotAuthor", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRecipeComment_ByRecipeOwnerWithExistingRecipe_DeletesComment() {
        var ownerId = UserId.New();
        var recipe = Recipe.Create(ownerId, "Owned recipe", 1);
        var comment = RecipeComment.Create(UserId.New(), recipe.Id, "Text");
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(comment);

        var handler = new DeleteRecipeCommentCommandHandler(repo, CreateRecipeRepository(recipe));
        Result result = await handler.Handle(
            new DeleteRecipeCommentCommand(ownerId.Value, recipe.Id.Value, comment.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(await repo.GetByIdAsync(comment.Id));
    }

    [Fact]
    public async Task GetRecipeComments_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetRecipeCommentsQueryHandler(new InMemoryRecipeCommentRepository());

        Result<PagedResponse<RecipeCommentModel>> result = await handler.Handle(
            new GetRecipeCommentsQuery(Guid.Empty, Guid.NewGuid(), 1, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetRecipeComments_ReturnsPagedCommentsAndOwnershipFlag() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(RecipeComment.Create(userId, recipeId, "Mine"));
        repo.Seed(RecipeComment.Create(UserId.New(), recipeId, "Other"));
        repo.Seed(RecipeComment.Create(userId, RecipeId.New(), "Different recipe"));
        var handler = new GetRecipeCommentsQueryHandler(repo);

        Result<PagedResponse<RecipeCommentModel>> result = await handler.Handle(
            new GetRecipeCommentsQuery(userId.Value, recipeId.Value, 0, 0),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Single(result.Value.Data);
        Assert.True(result.Value.Data[0].IsOwnedByCurrentUser);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryRecipeCommentRepository : IRecipeCommentRepository {
        private readonly List<RecipeComment> _comments = [];

        public void Seed(RecipeComment comment) => _comments.Add(comment);

        public Task<RecipeComment> AddAsync(RecipeComment comment, CancellationToken ct = default) {
            _comments.Add(comment);
            return Task.FromResult(comment);
        }

        public Task<RecipeComment?> GetByIdAsync(RecipeCommentId id, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_comments.FirstOrDefault(c => c.Id == id));

        public Task UpdateAsync(RecipeComment comment, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(RecipeComment comment, CancellationToken ct = default) {
            _comments.Remove(comment);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<RecipeComment> Items, int Total)> GetPagedByRecipeAsync(
            RecipeId recipeId, int page, int limit, CancellationToken ct = default) {
            var matching = _comments.Where(c => c.RecipeId == recipeId).ToList();
            var items = matching.Skip((page - 1) * limit).Take(limit).ToList();
            return Task.FromResult<(IReadOnlyList<RecipeComment>, int)>((items, matching.Count));
        }
    }

    private static INotificationRepository CreateNotificationRepository() =>
        CreateNotificationRepository(out _);

    private static INotificationRepository CreateNotificationRepository(out List<Notification> addedNotifications) {
        addedNotifications = [];
        List<Notification> capturedNotifications = addedNotifications;

        INotificationRepository repository = Substitute.For<INotificationRepository>();
        repository
            .AddAsync(Arg.Do<Notification>(capturedNotifications.Add), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<Notification>()));
        repository
            .ExistsAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.ArgAt<UserId>(0);
                string type = call.ArgAt<string>(1);
                string referenceId = call.ArgAt<string>(2);
                return Task.FromResult(capturedNotifications.Any(notification =>
                    notification.UserId == userId &&
                    string.Equals(notification.Type, type, StringComparison.Ordinal) &&
                    string.Equals(notification.ReferenceId, referenceId, StringComparison.Ordinal)));
            });
        return repository;
    }

    private static IRecipeRepository CreateRecipeRepository(Recipe? recipe) {
        IRecipeRepository repository = Substitute.For<IRecipeRepository>();
        repository
            .GetByIdAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(recipe));
        return repository;
    }
}
