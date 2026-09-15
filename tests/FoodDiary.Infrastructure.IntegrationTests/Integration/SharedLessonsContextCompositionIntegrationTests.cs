using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Lessons.Infrastructure;
using FoodDiary.Modules.Lessons.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedLessonsContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ModelContainsOnlyTwoOwnedEntities() {
        using var context = new LessonsDbContext(new DbContextOptionsBuilder<LessonsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Type[] expected = [typeof(NutritionLesson), typeof(UserLessonProgress)];
        Assert.Equal(expected.OrderBy(type => type.Name, StringComparer.Ordinal),
            context.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.Multiple(
            () => Assert.Equal("NutritionLessons", context.Model.FindEntityType(typeof(NutritionLesson))!.GetTableName()),
            () => Assert.Equal("UserLessonProgress", context.Model.FindEntityType(typeof(UserLessonProgress))!.GetTableName()));
    }

    [RequiresDockerFact]
    public async Task SharedSavePreservesPublicationProgressAndCascadeAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        LessonsDbContext lessons = provider.GetRequiredService<LessonsDbContext>();
        INutritionLessonRepository repository = provider.GetRequiredService<INutritionLessonRepository>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"lessons-context-{Guid.NewGuid():N}@example.com", "hash");
        var lesson = NutritionLesson.Create("Draft", "Content", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, estimatedReadMinutes: 1);
        lesson.SetPublication(isPublished: false);
        central.Users.Add(user);
        await repository.AddAsync(lesson);
        await repository.AddProgressAsync(UserLessonProgress.Create(user.Id, lesson.Id, DateTime.UtcNow));
        Assert.Same(central.Database.GetDbConnection(), lessons.Database.GetDbConnection());
        Assert.Empty(central.ChangeTracker.Entries<NutritionLesson>());
        Assert.Empty(central.ChangeTracker.Entries<UserLessonProgress>());
        await unitOfWork.SaveChangesAsync();
        lessons.ChangeTracker.Clear();
        Assert.Empty(await repository.GetByLocaleAsync("en"));
        Assert.Equal(1, Assert.Single(await repository.GetAdminReadModelsAsync()).CompletedCount);
        NutritionLesson? tracked = await repository.GetByIdTrackingAsync(lesson.Id);
        Assert.NotNull(tracked);
        tracked.SetPublication(isPublished: true);
        await unitOfWork.SaveChangesAsync();
        Assert.Single(await repository.GetByLocaleAsync("en"));
        Assert.Empty(await repository.GetByLocaleAsync("ru"));
        Assert.Equal(1, await repository.CountReadLessonsByLocaleAsync(user.Id, "en"));
        central.Users.Remove(user);
        await unitOfWork.SaveChangesAsync();
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.UserLessonProgress.AnyAsync(item => item.UserId == user.Id));
        Assert.True(await read.NutritionLessons.AnyAsync(item => item.Id == lesson.Id));
    }

    [RequiresDockerFact]
    public async Task InvalidProgressOwnerRollsBackCentralUserAndLessonAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        var user = User.Create($"lessons-rollback-{Guid.NewGuid():N}@example.com", "hash");
        var lesson = NutritionLesson.Create("Lesson", "Content", summary: null, "en",
            LessonCategory.NutritionBasics, LessonDifficulty.Beginner, estimatedReadMinutes: 1);
        central.Users.Add(user);
        INutritionLessonRepository repository = provider.GetRequiredService<INutritionLessonRepository>();
        await repository.AddAsync(lesson);
        await repository.AddProgressAsync(UserLessonProgress.Create(UserId.New(), lesson.Id, DateTime.UtcNow));
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(item => item.Id == user.Id));
        Assert.False(await read.NutritionLessons.AnyAsync(item => item.Id == lesson.Id));
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddLessonsModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
