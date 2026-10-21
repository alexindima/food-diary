using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class VerifiedCommitRecoveryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ConfirmedAbort_RethrowsTheOriginalCommitFailureInsteadOfAnUnknownOutcome() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using IDbContextTransaction provider = await database.Database.BeginTransactionAsync();
        var commitFailure = new IOException("The commit was not sent.");
        IDbContextTransaction transaction = CreateFaultingCommit(provider, commitFailure);
        transaction.DisposeAsync().Returns(_ => provider.DisposeAsync());

        IOException result = await Assert.ThrowsAsync<IOException>(() => VerifiedTransactionCommit.CommitAsync(transaction, CancellationToken.None));

        Assert.Same(commitFailure, result);
        _ = transaction.Received(1).CommitAsync(CancellationToken.None);
        _ = transaction.Received(1).DisposeAsync();
    }

    [RequiresDockerFact]
    public async Task StillActiveTransactionAfterDisposalFailure_RemainsUnknownAndDoesNotRepeatCommit() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using IDbContextTransaction provider = await database.Database.BeginTransactionAsync();
        var commitFailure = new IOException("The commit outcome is unavailable.");
        IDbContextTransaction transaction = CreateFaultingCommit(provider, commitFailure);
        transaction.DisposeAsync().Returns(ValueTask.FromException(new IOException("Disposal could not complete.")));

        CommitOutcomeUnknownException result = await Assert.ThrowsAsync<CommitOutcomeUnknownException>(() =>
            VerifiedTransactionCommit.CommitAsync(transaction, CancellationToken.None));

        Assert.Same(commitFailure, result.InnerException);
        _ = transaction.Received(1).CommitAsync(CancellationToken.None);
        _ = transaction.Received(1).DisposeAsync();
        await provider.RollbackAsync();
    }

    [RequiresDockerFact]
    public async Task UnavailableVerificationConnection_PreservesBothFailuresAndCannotAuthorizeReplay() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using IDbContextTransaction provider = await database.Database.BeginTransactionAsync();
        var source = (NpgsqlConnection)database.Database.GetDbConnection();
        var commitFailure = new IOException("The commit outcome is unavailable.");
        IDbContextTransaction transaction = CreateFaultingCommit(provider, commitFailure);
        using var unavailable = new TcpListener(IPAddress.Loopback, 0);
        unavailable.Start();
        int port = ((IPEndPoint)unavailable.LocalEndpoint).Port;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Task rejectConnection = RejectConnectionAsync(unavailable, cancellation.Token);
        transaction.DisposeAsync().Returns(_ => new ValueTask(DisposeAndReplaceEndpointAsync()));

        CommitOutcomeUnknownException result = await Assert.ThrowsAsync<CommitOutcomeUnknownException>(() =>
            VerifiedTransactionCommit.CommitAsync(transaction, cancellation.Token));

        AggregateException failures = Assert.IsType<AggregateException>(result.InnerException);
        Assert.Equal(2, failures.InnerExceptions.Count);
        Assert.Same(commitFailure, failures.InnerExceptions[0]);
        Assert.IsAssignableFrom<NpgsqlException>(failures.InnerExceptions[1]);
        await rejectConnection.WaitAsync(TimeSpan.FromSeconds(5));
        _ = transaction.Received(1).CommitAsync(cancellation.Token);

        async Task DisposeAndReplaceEndpointAsync() {
            await provider.DisposeAsync().ConfigureAwait(false);
            await source.CloseAsync().ConfigureAwait(false);
            source.ConnectionString = new NpgsqlConnectionStringBuilder {
                Host = IPAddress.Loopback.ToString(),
                Port = port,
                Username = "fixture",
                Password = "fixture",
                Database = "fixture",
                Pooling = false,
                Timeout = 2,
            }.ConnectionString;
        }
    }

    private static IDbContextTransaction CreateFaultingCommit(IDbContextTransaction provider, Exception commitFailure) {
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction, IInfrastructure<DbTransaction>>();
        ((IInfrastructure<DbTransaction>)transaction).Instance.Returns(provider.GetDbTransaction());
        transaction.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(commitFailure));
        return transaction;
    }

    private static async Task RejectConnectionAsync(TcpListener listener, CancellationToken cancellationToken) {
        using TcpClient connection = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
    }
}
