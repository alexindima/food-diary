using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.UpdateRecipeComment;
using FoodDiary.Application.RecipeComments.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Recipes;

using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.RecipeComments;

public class RecipeCommentsFeatureTests {
    [Fact]
    public async Task CreateRecipeComment_WithValidData_Succeeds() {
        var userId = UserId.New();
        var ownerId = UserId.New();
        var recipe = Recipe.Create(ownerId, "Pasta", 1);
        var commentRepo = new InMemoryRecipeCommentRepository();
        var notifRepo = new RecordingNotificationRepository();

        var handler = new CreateRecipeCommentCommandHandler(
            commentRepo,
            new StubRecipeRepository(recipe),
            notifRepo);
        var result = await handler.Handle(
            new CreateRecipeCommentCommand(userId.Value, recipe.Id.Value, "Delicious!"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Delicious!", result.Value.Text);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Single(notifRepo.Added); // notification for recipe owner
    }

    [Fact]
    public async Task CreateRecipeComment_OnOwnRecipe_DoesNotCreateNotification() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Pasta", 1);
        var notifRepo = new RecordingNotificationRepository();

        var handler = new CreateRecipeCommentCommandHandler(
            new InMemoryRecipeCommentRepository(),
            new StubRecipeRepository(recipe),
            notifRepo);
        var result = await handler.Handle(
            new CreateRecipeCommentCommand(userId.Value, recipe.Id.Value, "My note"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(notifRepo.Added);
    }

    [Fact]
    public async Task CreateRecipeComment_WhenRecipeNotFound_ReturnsFailure() {
        var handler = new CreateRecipeCommentCommandHandler(
            new InMemoryRecipeCommentRepository(),
            new StubRecipeRepository(null),
            new RecordingNotificationRepository());

        var result = await handler.Handle(
            new CreateRecipeCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "Text"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateRecipeComment_ByAuthor_Succeeds() {
        var userId = UserId.New();
        var comment = RecipeComment.Create(userId, RecipeId.New(), "Old text");
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(comment);

        var handler = new UpdateRecipeCommentCommandHandler(repo);
        var result = await handler.Handle(
            new UpdateRecipeCommentCommand(userId.Value, comment.Id.Value, "New text"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
        var result = await handler.Handle(
            new UpdateRecipeCommentCommand(otherUserId.Value, comment.Id.Value, "Hacked"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotAuthor", result.Error.Code);
    }

    [Fact]
    public async Task DeleteRecipeComment_ByAuthor_Succeeds() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var comment = RecipeComment.Create(userId, recipeId, "Text");
        var repo = new InMemoryRecipeCommentRepository();
        repo.Seed(comment);

        var handler = new DeleteRecipeCommentCommandHandler(repo, new StubRecipeRepository(null));
        var result = await handler.Handle(
            new DeleteRecipeCommentCommand(userId.Value, recipeId.Value, comment.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteRecipeComment_CommentNotFound_ReturnsFailure() {
        var handler = new DeleteRecipeCommentCommandHandler(
            new InMemoryRecipeCommentRepository(), new StubRecipeRepository(null));

        var result = await handler.Handle(
            new DeleteRecipeCommentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

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
            var items = _comments.Where(c => c.RecipeId == recipeId).ToList();
            return Task.FromResult<(IReadOnlyList<RecipeComment>, int)>((items, items.Count));
        }
    }

    private sealed class RecordingNotificationRepository : INotificationRepository {
        public List<Notification> Added { get; } = [];

        public Task<Notification> AddAsync(Notification notification, CancellationToken ct = default) {
            Added.Add(notification);
            return Task.FromResult(notification);
        }

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken ct = default) =>
            Task.FromResult(Added.Any(n => n.UserId == userId && n.Type == type && n.ReferenceId == referenceId));

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Notification notification, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MarkAllReadAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubRecipeRepository(Recipe? recipe) : IRecipeRepository {
        public Task<Recipe?> GetByIdAsync(RecipeId id, UserId userId, bool includePublic = true,
            bool includeSteps = false, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(recipe);

        public Task<Recipe> AddAsync(Recipe r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Recipe r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Recipe r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateNutritionAsync(Recipe r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(UserId userId, bool includePublic, int page, int limit, string? search, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(IEnumerable<RecipeId> ids, UserId userId, bool includePublic = true, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(IEnumerable<RecipeId> ids, UserId userId, bool includePublic = true, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
