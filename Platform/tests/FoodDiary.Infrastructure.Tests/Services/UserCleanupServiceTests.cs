using System.Data.Common;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class UserCleanupServiceTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_RejectsMissingOrDuplicatePurgeOrdering(bool duplicate) {
        IUserDataPurgeParticipant[] participants = duplicate
            ? [Substitute.For<IUserDataPurgeParticipant>(), Substitute.For<IUserDataPurgeParticipant>()] : [];
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            new UserCleanupService(dbContext: null!, participants, NullLogger<UserCleanupService>.Instance, transactionCoordinator: Substitute.For<IModuleTransactionCoordinator>(), scopeGuard: Substitute.For<IModuleScopeGuard>()));
        Assert.Contains("unique ordering", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CleanupDeletedUsersAsync_WithNonPositiveBatchSize_Throws() {
        var service = new UserCleanupService(dbContext: null!, participants: [Substitute.For<IUserDataPurgeParticipant>()], logger: NullLogger<UserCleanupService>.Instance, transactionCoordinator: Substitute.For<IModuleTransactionCoordinator>(), scopeGuard: Substitute.For<IModuleScopeGuard>());

        ArgumentOutOfRangeException ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CleanupDeletedUsersAsync(DateTime.UtcNow, 0, reassignUserId: null, CancellationToken.None));

        Assert.Equal("batchSize", ex.ParamName);
    }

    [Fact]
    public async Task CleanupDeletedUsersAsync_WhenCleanupUserFails_ContinuesAndReturnsZeroRemoved() {
        await using UsersDbContext context = CreateInMemoryContext();
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        context.Users.Add(deletedUser);
        await context.SaveChangesAsync();
        IModuleTransactionCoordinator coordinator = Substitute.For<IModuleTransactionCoordinator>();
        coordinator.ExecuteItemAsync(Arg.Any<Func<DbTransaction, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Injected item failure.")));
        var service = new UserCleanupService(context, [Substitute.For<IUserDataPurgeParticipant>()], NullLogger<UserCleanupService>.Instance,
            coordinator, Substitute.For<IModuleScopeGuard>());

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

    private static UsersDbContext CreateInMemoryContext() {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new UsersDbContext(options);
    }

    private static T InvokePrivateStatic<T>(string methodName, params object[] args) {
        MethodInfo method = typeof(UserCleanupService).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return (T)method.Invoke(null, args)!;
    }

}
