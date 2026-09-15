using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedBillingContextIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CheckoutLease_RemainsHeldAfterSharedTransactionEndsAsync(bool commit) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        IBillingCheckoutLock checkoutLock = provider.GetRequiredService<IBillingCheckoutLock>();
        var userId = Guid.NewGuid();
        IAsyncDisposable lease = await checkoutLock.AcquireAsync(userId);
        await using (lease) {
            Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
            await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync()) {
                if (commit) {
                    await transaction.CommitAsync();
                } else {
                    await transaction.RollbackAsync();
                }
            }

            await using var probe = new NpgsqlConnection(context.Database.GetConnectionString());
            await probe.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", probe);
            command.Parameters.AddWithValue("key", BitConverter.ToInt64(userId.ToByteArray(), 0));
            Assert.Equal(false, await command.ExecuteScalarAsync());
        }

        // Disposal is idempotent and the same key can immediately be acquired again.
        await lease.DisposeAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using IAsyncDisposable nextLease = await checkoutLock.AcquireAsync(userId, timeout.Token);
    }

    [RequiresDockerFact]
    public async Task CheckoutLease_CanceledWaitDoesNotReleaseExistingHolderAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        IBillingCheckoutLock checkoutLock = provider.GetRequiredService<IBillingCheckoutLock>();
        var userId = Guid.NewGuid();
        await using (IAsyncDisposable holder = await checkoutLock.AcquireAsync(userId)) {
            using var canceledWait = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => checkoutLock.AcquireAsync(userId, canceledWait.Token));

            await using var probe = new NpgsqlConnection(context.Database.GetConnectionString());
            await probe.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", probe);
            command.Parameters.AddWithValue("key", BitConverter.ToInt64(userId.ToByteArray(), 0));
            Assert.Equal(false, await command.ExecuteScalarAsync());
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using IAsyncDisposable nextLease = await checkoutLock.AcquireAsync(userId, timeout.Token);
    }
}
