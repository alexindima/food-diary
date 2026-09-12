using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.RecipeComments;
using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Fasting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserRelatedConsumerIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task CommentPage_BatchesOnlyPageAuthorsAndDoesNotRefillMissingAuthors() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("comment-page@example.com", "hash");
        var other = User.Create("comment-other@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "Recipe", servings: 1);
        context.Users.AddRange(user, other);
        context.Recipes.Add(recipe);
        RecipeComment[] comments = [.. Enumerable.Range(0, 5).Select(index => RecipeComment.Create(
            index == 2 ? other.Id : user.Id, recipe.Id, FormattableString.Invariant($"Comment {index}")))];
        context.RecipeComments.AddRange(comments);
        await context.SaveChangesAsync();
        DateTime now = DateTime.UtcNow;
        for (int index = 0; index < comments.Length; index++) {
            RecipeCommentId id = comments[index].Id;
            DateTime created = now.AddMinutes(-index);
            await context.RecipeComments.Where(comment => comment.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(comment => comment.CreatedOnUtc, created));
        }
        context.ChangeTracker.Clear();
        IUserCommentAuthorReadService users = Substitute.For<IUserCommentAuthorReadService>();
        users.GetAuthorsAsync(Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<UserId, UserCommentAuthorModel>>(
                new Dictionary<UserId, UserCommentAuthorModel> { [user.Id] = new("author", "Name") }));
        var repository = new RecipeCommentRepository(context, users);
        (IReadOnlyList<RecipeCommentReadModel> items, int total) = await repository.GetPagedReadModelsByRecipeAsync(recipe.Id, 2, 2);

        Assert.Equal(5, total);
        Assert.Equal("Comment 3", Assert.Single(items).Text);
        await users.Received(1).GetAuthorsAsync(Arg.Is<IReadOnlyCollection<UserId>>(
            ids => ids.Count == 2 && ids.Contains(user.Id) && ids.Contains(other.Id)), Arg.Any<CancellationToken>());
        Assert.Empty(context.ChangeTracker.Entries());
        users.ClearReceivedCalls();
        Assert.Empty((await repository.GetPagedReadModelsByRecipeAsync(recipe.Id, 10, 2)).Items);
        Assert.Empty(users.ReceivedCalls());
    }

    [RequiresDockerFact]
    public async Task FastingActive_BatchesDistinctUsersAndKeepsOrderingPlanAndMissingUserSemantics() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("fasting-batch@example.com", "hash");
        var other = User.Create("fasting-other@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.Fast16Eat8, 16, 8, now.AddDays(-3), "Plan");
        context.Users.AddRange(user, other);
        context.FastingPlans.Add(plan);
        var first = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, now.AddHours(-4), 1, 16);
        var second = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, now.AddHours(-2), 2, 16);
        var missing = FastingOccurrence.Create(plan.Id, other.Id, FastingOccurrenceKind.FastingWindow, now.AddHours(-3), 3, 16);
        context.FastingOccurrences.AddRange(second, missing, first);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        IUserFastingReminderReadService users = Substitute.For<IUserFastingReminderReadService>();
        users.GetReminderSettingsAsync(Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<UserId, UserFastingReminderModel>>(
                new Dictionary<UserId, UserFastingReminderModel> { [user.Id] = new(6, 10) }));

        IReadOnlyList<FastingActiveOccurrenceModel> rows = await new FastingOccurrenceRepository(context, users).GetActiveAsync();

        Assert.Equal(new[] { first.Id, second.Id }, rows.Select(row => row.Occurrence.Id));
        Assert.All(rows, row => {
            Assert.Equal("Plan", row.Occurrence.Plan.Title);
            Assert.Equal(6, row.ReminderHours);
            Assert.Equal(10, row.FollowUpReminderHours);
        });
        await users.Received(1).GetReminderSettingsAsync(Arg.Is<IReadOnlyCollection<UserId>>(
            ids => ids.Count == 2 && ids.Contains(user.Id) && ids.Contains(other.Id)), Arg.Any<CancellationToken>());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task EmptyFastingAndCancelledReads_DoNotInvokeUserReader() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        IUserFastingReminderReadService users = Substitute.For<IUserFastingReminderReadService>();
        var repository = new FastingOccurrenceRepository(context, users);
        Assert.Empty(await repository.GetActiveAsync());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetActiveAsync(cancelled.Token));
        Assert.Empty(users.ReceivedCalls());
        var comments = new RecipeCommentRepository(context, new UserRelatedDataReadService(context));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => comments.GetPagedReadModelsByRecipeAsync(
            RecipeId.New(), 1, 10, cancelled.Token));
    }
}
