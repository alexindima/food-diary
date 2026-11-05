using System.Data.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class PostgresBootstrapRetryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task TransientMigrationConnectionFailure_RetriesBeforeApplicationQueries() {
        var interceptor = new FailFirstConnection(transient: true);

        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync(interceptor);

        Assert.True(interceptor.OpenAttempts > 1);
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
        Assert.False(context.Database.CreateExecutionStrategy().RetriesOnFailure);
        int bootstrapAttempts = interceptor.OpenAttempts;
        _ = await context.Users.CountAsync();
        Assert.Equal(bootstrapAttempts, interceptor.OpenAttempts);
    }

    [RequiresDockerFact]
    public async Task NonTransientMigrationConnectionFailure_IsNotRetried() {
        var interceptor = new FailFirstConnection(transient: false);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            databaseFixture.CreateDbContextAsync(interceptor));

        Assert.Multiple(
            () => Assert.Equal("Synthetic non-transient migration failure", error.Message),
            () => Assert.Equal(1, interceptor.OpenAttempts));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailFirstConnection(bool transient) : DbConnectionInterceptor {
        public int OpenAttempts { get; private set; }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) {
            OpenAttempts++;
            if (OpenAttempts == 1) {
                if (transient) {
                    throw new NpgsqlException("Synthetic migration connection timeout", new TimeoutException());
                }
                throw new InvalidOperationException("Synthetic non-transient migration failure");
            }
            return ValueTask.FromResult(result);
        }
    }
}
