using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class UserCleanupServiceTests {
    [Fact]
    public async Task CleanupDeletedUsersAsync_WithNonPositiveBatchSize_Throws() {
        var service = new UserCleanupService(dbContext: null!, imageObjectDeletionOutbox: CreateImageObjectDeletionOutbox(), logger: NullLogger<UserCleanupService>.Instance);

        ArgumentOutOfRangeException ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CleanupDeletedUsersAsync(DateTime.UtcNow, 0, reassignUserId: null, CancellationToken.None));

        Assert.Equal("batchSize", ex.ParamName);
    }

    [Fact]
    public async Task CleanupDeletedUsersAsync_WhenCleanupUserFails_ContinuesAndReturnsZeroRemoved() {
        await using FoodDiaryDbContext context = CreateInMemoryContext();
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        context.Users.Add(deletedUser);
        await context.SaveChangesAsync();
        var service = new UserCleanupService(context, CreateImageObjectDeletionOutbox(), NullLogger<UserCleanupService>.Instance);

        int removed = await service.CleanupDeletedUsersAsync(
            DateTime.UtcNow.AddDays(-1),
            batchSize: 10,
            reassignUserId: null);

        Assert.Equal(0, removed);
    }

    [Fact]
    public void NormalizeUtc_NormalizesUtcLocalAndUnspecifiedValues() {
        var utc = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);
        var local = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal(utc, InvokePrivateStatic<DateTime>("NormalizeUtc", utc));
        Assert.Equal(local.ToUniversalTime(), InvokePrivateStatic<DateTime>("NormalizeUtc", local));
        DateTime normalizedUnspecified = InvokePrivateStatic<DateTime>("NormalizeUtc", unspecified);
        Assert.Equal(DateTimeKind.Utc, normalizedUnspecified.Kind);
        Assert.Equal(unspecified, DateTime.SpecifyKind(normalizedUnspecified, DateTimeKind.Unspecified));
    }

    private static FoodDiaryDbContext CreateInMemoryContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private static IImageObjectDeletionOutbox CreateImageObjectDeletionOutbox() {
        IImageObjectDeletionOutbox outbox = Substitute.For<IImageObjectDeletionOutbox>();
        outbox.EnqueueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return outbox;
    }

    private static T InvokePrivateStatic<T>(string methodName, params object[] args) {
        MethodInfo method = typeof(UserCleanupService).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return (T)method.Invoke(null, args)!;
    }

}
