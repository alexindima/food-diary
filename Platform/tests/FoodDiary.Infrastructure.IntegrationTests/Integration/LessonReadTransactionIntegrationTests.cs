using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Gamification.Contracts.Achievements.Common;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Application.Commands.MarkLessonRead;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Modules.Lessons.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class LessonReadTransactionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MarkRead_ProgressAndEvaluationCommitOrRollbackTogether(bool failAfterEnqueue) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        (User user, NutritionLesson lesson) = await SeedAsync(context);
        await using ServiceProvider provider = CreateProvider(context);
        IAchievementEvaluationOutbox actual = provider.GetRequiredService<IAchievementEvaluationOutbox>();
        IAchievementEvaluationOutbox observed = Substitute.For<IAchievementEvaluationOutbox>();
        observed.EnqueueAsync(user.Id, Arg.Any<CancellationToken>()).Returns(async call => {
            await actual.EnqueueAsync(user.Id, call.Arg<CancellationToken>());
            await using FoodDiaryDbContext outside = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
            Assert.False(await outside.AchievementEvaluationOutbox.AnyAsync(row => row.UserId == user.Id));
            Assert.False(await outside.UserLessonProgress.AnyAsync(row => row.UserId == user.Id));
            if (failAfterEnqueue) { throw new InvalidOperationException("Abort before progress save"); }
        });
        MarkLessonReadCommandHandler handler = CreateHandler(provider, observed);
        var command = new MarkLessonReadCommand(user.Id.Value, lesson.Id.Value);

        if (failAfterEnqueue) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        } else {
            Assert.True((await handler.Handle(command, CancellationToken.None)).IsSuccess);
        }

        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        Assert.Equal(failAfterEnqueue ? 0 : 1, await read.UserLessonProgress.CountAsync(row => row.UserId == user.Id));
        Assert.Equal(failAfterEnqueue ? 0 : 1, await read.AchievementEvaluationOutbox.CountAsync(row => row.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task ConcurrentMarkRead_ReturnsSuccessAndCreatesOneProgressAndOneEvaluation() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        (User user, NutritionLesson lesson) = await SeedAsync(context);
        await using FoodDiaryDbContext other = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider firstProvider = CreateProvider(context);
        await using ServiceProvider secondProvider = CreateProvider(other);
        var enqueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        IAchievementEvaluationOutbox actual = firstProvider.GetRequiredService<IAchievementEvaluationOutbox>();
        IAchievementEvaluationOutbox paused = Substitute.For<IAchievementEvaluationOutbox>();
        paused.EnqueueAsync(user.Id, Arg.Any<CancellationToken>()).Returns(async call => {
            await actual.EnqueueAsync(user.Id, call.Arg<CancellationToken>());
            enqueued.TrySetResult();
            await release.Task.WaitAsync(timeout.Token);
        });
        var command = new MarkLessonReadCommand(user.Id.Value, lesson.Id.Value);
        Task<Result> first = CreateHandler(firstProvider, paused).Handle(command, timeout.Token);
        await enqueued.Task.WaitAsync(timeout.Token);
        Task<Result> second = CreateHandler(secondProvider, secondProvider.GetRequiredService<IAchievementEvaluationOutbox>()).Handle(command, timeout.Token);
        release.TrySetResult();
        Result[] results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        Assert.Equal(1, await read.UserLessonProgress.CountAsync(row => row.UserId == user.Id && row.LessonId == lesson.Id));
        Assert.Equal(1, (await read.AchievementEvaluationOutbox.SingleAsync(row => row.UserId == user.Id)).Revision);
    }

    private static MarkLessonReadCommandHandler CreateHandler(IServiceProvider provider, IAchievementEvaluationOutbox outbox) =>
        new(provider.GetRequiredService<INutritionLessonReadRepository>(), provider.GetRequiredService<INutritionLessonWriteRepository>(),
            TimeProvider.System, Substitute.For<ICurrentUserAccessService>(), outbox, provider.GetRequiredService<ILessonProgressTransactionRunner>());

    private static async Task<(User User, NutritionLesson Lesson)> SeedAsync(FoodDiaryDbContext context) {
        var user = User.Create($"lesson-transaction-{Guid.NewGuid():N}@example.com", "hash");
        var lesson = NutritionLesson.Create("Lesson", "Content", summary: null, "en", LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 1);
        context.Users.Add(user);
        context.NutritionLessons.Add(lesson);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return (user, lesson);
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddSingleton(TimeProvider.System);
        services.AddLessonsModule();
        services.AddGamificationModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(FoodDiary.Domain.Primitives.IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
