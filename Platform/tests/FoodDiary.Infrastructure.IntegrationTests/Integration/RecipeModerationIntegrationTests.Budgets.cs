using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class RecipeModerationIntegrationTests {
    [RequiresDockerTheory]
    [InlineData("RequestsPerMinute")]
    [InlineData("RequestsPerDay")]
    public async Task RequestLargerThanEitherBudgetIsNotSentAsync(string budgetLimit) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        var recipe = Recipe.Create(author.Id, "Private soup", servings: 2, visibility: Visibility.Private);
        var photo = ImageAsset.Create(author.Id, "moderation/budget.jpg", "https://cdn.example.com/budget.jpg");
        database.AddRange(recipe, photo);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, privateAssets: [photo]);
        provider.GetRequiredService<IConfiguration>()[$"RecipeModeration:OpenAi:{budgetLimit}"] = "1";
        Guid revisionId = GetRevisionId(await ReplacePendingAsync(provider, author.Id, recipe.Id.Value, "Photo soup", [photo.Id.Value]));

        Assert.True(await ProcessAsync(provider));

        RecipePublicationRevision delayed = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.CheckDelayed, delayed.Status),
            () => Assert.Equal("CheckUnavailable", delayed.Reason),
            () => Assert.Null(delayed.NextAttemptAt),
            () => Assert.Null(delayed.PrimaryResult));
        await openAi.DidNotReceive().CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());
        Assert.False(await database.Set<RecipeModerationBudget>().AnyAsync());
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BudgetDelaysUseWindowBoundariesWithoutSpendingAnAttemptAsync(bool observation) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        IRecipeModerationProvider mistral = CreateProviderStub("Mistral");
        IRecipeModerationProvider selectedProvider = observation ? mistral : openAi;
        selectedProvider.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        string providerName = selectedProvider.Name;
        var day = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset minute = day.AddHours(12);
        var clock = new ModerationClock(minute.AddSeconds(30));
        database.AddRange(
            new RecipeModerationBudget { Provider = providerName, Requests = 2, WindowStart = day, WindowSeconds = 86_400 },
            new RecipeModerationBudget { Provider = providerName, Requests = 1, WindowStart = minute, WindowSeconds = 60 },
            new RecipeModerationBudget { Provider = providerName, Requests = 3, WindowStart = day.AddDays(-2), WindowSeconds = 86_400 });
        await database.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, mistral, clock: clock);
        IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
        configuration[$"RecipeModeration:{providerName}:RequestsPerMinute"] = "1";
        configuration[$"RecipeModeration:{providerName}:RequestsPerDay"] = "3";
        await PublishPolicyAsync(provider, author.Id, "harassment");
        Guid firstId = GetRevisionId(await CreatePendingAsync(provider, author.Id));

        Assert.True(await ProcessAsync(provider, observation));

        RecipePublicationRevision minuteDelayed = await ReadRevisionAsync(database, firstId);
        AssertBudgetDelay(minuteDelayed, observation, minute.AddMinutes(1));
        await selectedProvider.DidNotReceive().CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());
        Assert.False(await ProcessAsync(provider, observation));
        RecipeModerationBudget dayBudget = await database.Set<RecipeModerationBudget>().AsNoTracking()
            .SingleAsync(budget => budget.Provider == providerName && budget.WindowStart == day && budget.WindowSeconds == 86_400);
        Assert.Multiple(
            () => Assert.Equal(providerName, dayBudget.Provider),
            () => Assert.Equal(2, dayBudget.Requests),
            () => Assert.Equal(day, dayBudget.WindowStart),
            () => Assert.Equal(86_400, dayBudget.WindowSeconds));

        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(await ProcessAsync(provider, observation));
        if (!observation) {
            Assert.True(await ProcessAsync(provider));
        }
        Guid secondId = GetRevisionId(await CreatePendingAsync(provider, author.Id));
        Assert.True(await ProcessAsync(provider, observation));

        RecipePublicationRevision dayDelayed = await ReadRevisionAsync(database, secondId);
        AssertBudgetDelay(dayDelayed, observation, day.AddDays(1));
        await selectedProvider.Received(1).CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());
        Assert.False(await database.Set<RecipeModerationBudget>().AnyAsync(budget => budget.WindowStart < day));

        clock.Advance(day.AddDays(1) - clock.GetUtcNow());
        Assert.True(await ProcessAsync(provider, observation));

        RecipePublicationRevision checkedRevision = await ReadRevisionAsync(database, secondId);
        Assert.Multiple(
            () => Assert.Equal(1, observation ? checkedRevision.ShadowAttempts : checkedRevision.Attempts),
            () => Assert.NotNull(observation ? checkedRevision.ShadowResult : checkedRevision.PrimaryResult));
        await selectedProvider.Received(2).CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>());
    }

    private static void AssertBudgetDelay(RecipePublicationRevision revision, bool observation, DateTimeOffset expectedRetry) {
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.Pending, revision.Status),
            () => Assert.Equal(0, observation ? revision.ShadowAttempts : revision.Attempts),
            () => Assert.Equal(expectedRetry, observation ? revision.ShadowNextAttemptAt : revision.NextAttemptAt),
            () => Assert.Null(observation ? revision.ShadowLeaseToken : revision.LeaseToken));
    }
}
