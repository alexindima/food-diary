using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Runtime;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Mediator;
using FoodDiary.Modules.Gamification.Contracts.Achievements.Common;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Infrastructure;
using FoodDiary.Modules.Meals.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedDeliveryContextsIntegrationTests {
    [RequiresDockerTheory]
    [InlineData("commit")]
    [InlineData("failure")]
    [InlineData("throw")]
    [InlineData("cancel")]
    [InlineData("save-failure")]
    public async Task AtomicPipeline_KeepsMealAndEvaluationInvisibleUntilCommitAsync(string outcome) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"atomic-meal-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        await central.SaveChangesAsync();
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddApplicationRuntime();
        services.AddMealsPersistence();
        services.AddGamificationModule();
        services.AddSingleton(central);
        services.AddSingleton<SharedPersistenceDbContext>(central);
        services.AddSingleton<FoodDiary.Application.Abstractions.Common.Abstractions.Events.IDomainEventPublisher, NoEvents>();
        services.AddScoped<IRequestHandler<AtomicMealProbe, Result<Guid>>, AtomicMealProbeHandler>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using FoodDiaryDbContext observer = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        using var cancellation = new CancellationTokenSource();
        bool postCommitRan = false;
        var command = new AtomicMealProbe(user.Id, outcome, async () => {
            Assert.Empty(await observer.AchievementEvaluationOutbox.AsNoTracking().ToListAsync());
            Assert.Empty(await observer.Meals.AsNoTracking().ToListAsync());
            if (string.Equals(outcome, "cancel", StringComparison.Ordinal)) {
                await cancellation.CancelAsync();
            }
        }, () => postCommitRan = true);
        Task<Result<Guid>> execution = provider.GetRequiredService<ISender>().Send(command, cancellation.Token);
        switch (outcome) {
            case "throw":
                await Assert.ThrowsAsync<InvalidOperationException>(() => execution);
                break;
            case "cancel":
                await Assert.ThrowsAsync<OperationCanceledException>(() => execution);
                break;
            case "save-failure":
                await Assert.ThrowsAsync<DbUpdateException>(() => execution);
                break;
            default:
                Result<Guid> result = await execution;
                Assert.Equal(string.Equals(outcome, "commit", StringComparison.Ordinal), result.IsSuccess);
                break;
        }
        Assert.Equal(string.Equals(outcome, "commit", StringComparison.Ordinal), await observer.Meals.AnyAsync());
        Assert.Equal(string.Equals(outcome, "commit", StringComparison.Ordinal), await observer.AchievementEvaluationOutbox.AnyAsync());
        Assert.Equal(string.Equals(outcome, "commit", StringComparison.Ordinal), postCommitRan);
        Assert.False(provider.GetRequiredService<IUnitOfWork>().HasPendingChanges);
        Assert.False(provider.GetRequiredService<IPostCommitActionQueue>().HasActions);
    }

    [ExcludeFromCodeCoverage]
    private sealed record AtomicMealProbe(UserId UserId, string Outcome, Func<Task> Observe, Action OnCommit)
        : ICommand<Result<Guid>>, IAtomicCommand;

    [ExcludeFromCodeCoverage]
    private sealed class AtomicMealProbeHandler(
        MealsDbContext meals,
        IAchievementEvaluationOutbox outbox,
        IPostCommitActionQueue actions) : IRequestHandler<AtomicMealProbe, Result<Guid>> {
        public async Task<Result<Guid>> Handle(AtomicMealProbe request, CancellationToken cancellationToken) {
            var meal = Meal.Create(string.Equals(request.Outcome, "save-failure", StringComparison.Ordinal) ? UserId.New() : request.UserId,
                new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc));
            meals.Meals.Add(meal);
            await outbox.EnqueueAsync(request.UserId, cancellationToken);
            await request.Observe();
            actions.Enqueue("atomic-meal", _ => { request.OnCommit(); return Task.CompletedTask; });
            return request.Outcome switch {
                "failure" => Result.Failure<Guid>(new Error("Probe.Failed", "Rejected")),
                "throw" => throw new InvalidOperationException("Handler failed"),
                "cancel" => throw new OperationCanceledException(cancellationToken),
                _ => Result.Success(meal.Id.Value),
            };
        }
    }
}
