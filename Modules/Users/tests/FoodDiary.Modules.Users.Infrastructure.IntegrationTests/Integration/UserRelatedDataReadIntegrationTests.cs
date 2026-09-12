using FoodDiary.Application.Abstractions.Users.Models;
using System.Data.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserRelatedDataReadIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task BatchReads_KeepPersistedValuesAndAllAccountStatesWithoutTracking() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var active = User.Create("related-active@example.com", "hash");
        active.UpdatePersonalInfo(username: "author", firstName: "Name");
        active.UpdatePreferences(new UserPreferenceUpdate(FastingCheckInReminderHours: 6, FastingCheckInFollowUpReminderHours: 10));
        var inactive = User.Create("related-inactive@example.com", "hash");
        inactive.Deactivate();
        var deleted = User.Create("related-deleted@example.com", "hash");
        deleted.MarkDeleted(DateTime.UtcNow);
        seed.Users.AddRange(active, inactive, deleted);
        await seed.SaveChangesAsync();
        active.UpdatePersonalInfo(firstName: "Unsaved");
        var capture = new QueryCapture();
        await using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(seed.Database.GetConnectionString()).AddInterceptors(capture).Options);
        var reader = new UserRelatedDataReadService(context);
        UserId[] ids = [active.Id, inactive.Id, deleted.Id, active.Id, UserId.New()];

        IReadOnlyDictionary<UserId, UserCommentAuthorModel> authors = await reader.GetAuthorsAsync(ids);
        IReadOnlyDictionary<UserId, UserFastingReminderModel> reminders = await reader.GetReminderSettingsAsync(ids);

        Assert.Multiple(
            () => Assert.Equal(3, authors.Count),
            () => Assert.Equal(3, reminders.Count),
            () => Assert.Equal("Name", authors[active.Id].FirstName),
            () => Assert.Equal("author", authors[active.Id].Username),
            () => Assert.Null(authors[inactive.Id].FirstName),
            () => Assert.Equal(6, reminders[active.Id].ReminderHours),
            () => Assert.Equal(10, reminders[active.Id].FollowUpReminderHours),
            () => Assert.Equal(2, capture.Commands.Count),
            () => Assert.Empty(context.ChangeTracker.Entries()));
        foreach (string sql in capture.Commands) {
            foreach (string unrelated in new[] { "Password", "Email", "UserRoles", "WeightGoals", "WaistGoals" }) {
                Assert.DoesNotContain(unrelated, sql, StringComparison.Ordinal);
            }
        }

        Assert.Empty(await reader.GetAuthorsAsync([]));
        Assert.Empty(await reader.GetReminderSettingsAsync([]));
        Assert.Equal(2, capture.Commands.Count);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetAuthorsAsync(ids, cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetReminderSettingsAsync([], cancelled.Token));
    }

    [Fact]
    public void Registration_SharesBothNarrowReadersWithinScope() {
        var services = new ServiceCollection();
        services.AddScoped(_ => new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=test").Options));
        services.AddUsersPersistence();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IUserCommentAuthorReadService reader = first.ServiceProvider.GetRequiredService<IUserCommentAuthorReadService>();
        Assert.Same(reader, first.ServiceProvider.GetRequiredService<IUserFastingReminderReadService>());
        Assert.NotSame(reader, second.ServiceProvider.GetRequiredService<IUserCommentAuthorReadService>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class QueryCapture : DbCommandInterceptor {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
