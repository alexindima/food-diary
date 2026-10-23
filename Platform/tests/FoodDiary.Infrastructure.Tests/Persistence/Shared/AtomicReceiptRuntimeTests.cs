using System.Data.Common;
using System.Text.Json;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.PostgreSql;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.Tests.Persistence.Shared;

[ExcludeFromCodeCoverage]
public sealed class AtomicReceiptRuntimeTests {
    [Theory]
    [InlineData("owner")]
    [InlineData("key")]
    [InlineData("hash")]
    [InlineData("zero-retention")]
    [InlineData("negative-retention")]
    [InlineData("long-retention")]
    public async Task InvalidAtomicIdentity_IsRejectedBeforeEnteringTheCoordinator(string invalidField) {
        await using SharedRuntimeDbContext context = CreateGuardContext();
        IModuleTransactionCoordinator coordinator = Substitute.For<IModuleTransactionCoordinator>();
        var executor = new EfAtomicCommandExecutor(coordinator, context, TimeProvider.System);
        var valid = new AtomicOperation(Guid.NewGuid(), new string('A', 64), new string('B', 64), TimeSpan.FromDays(1));
        AtomicOperation identity = invalidField switch {
            "owner" => valid with { UserId = Guid.Empty },
            "key" => valid with { Key = new string('A', 63) },
            "hash" => valid with { RequestHash = new string('B', 63) },
            "zero-retention" => valid with { Retention = TimeSpan.Zero },
            "negative-retention" => valid with { Retention = TimeSpan.FromTicks(-1) },
            _ => valid with { Retention = TimeSpan.FromDays(7).Add(TimeSpan.FromTicks(1)) },
        };

        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteAsync(identity, _ => Task.FromResult(1)));

        Assert.Equal("identity", error.ParamName);
        Assert.Empty(coordinator.ReceivedCalls());
    }

    [Fact]
    public async Task NullAtomicIdentity_IsRejectedBeforeEnteringTheCoordinator() {
        await using SharedRuntimeDbContext context = CreateGuardContext();
        IModuleTransactionCoordinator coordinator = Substitute.For<IModuleTransactionCoordinator>();
        var executor = new EfAtomicCommandExecutor(coordinator, context, TimeProvider.System);

        ArgumentNullException error = await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync(identity: null!, _ => Task.FromResult(1)));

        Assert.Equal("identity", error.ParamName);
        Assert.Empty(coordinator.ReceivedCalls());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task InvalidSnapshotBudget_IsRejectedBeforeInvokingTheOperation(int timeoutSeconds, int queries) {
        await using SharedRuntimeDbContext context = CreateGuardContext();
        var interceptor = new ReadSnapshotBudgetInterceptor();
        var executor = new EfReadSnapshotExecutor(context, interceptor, TimeProvider.System);
        bool invoked = false;

        ArgumentOutOfRangeException error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => executor.ExecuteAsync(
            new ReadSnapshotBudget(TimeSpan.FromSeconds(timeoutSeconds), queries), _ => {
                invoked = true;
                return Task.FromResult(1);
            }));

        Assert.Equal("budget", error.ParamName);
        Assert.False(invoked);
        Assert.Null(interceptor.MaximumQueries);
        Assert.Equal(0, interceptor.QueryCount);
    }

    [Fact]
    public async Task NullSnapshotOperation_IsRejectedBeforeOpeningTheDatabase() {
        await using SharedRuntimeDbContext context = CreateGuardContext();
        var interceptor = new ReadSnapshotBudgetInterceptor();
        var executor = new EfReadSnapshotExecutor(context, interceptor, TimeProvider.System);

        ArgumentNullException error = await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync<int>(
            new ReadSnapshotBudget(TimeSpan.FromSeconds(1), 1), operation: null!));

        Assert.Equal("operation", error.ParamName);
        Assert.Null(interceptor.MaximumQueries);
        Assert.Equal(0, interceptor.QueryCount);
    }

    private static SharedRuntimeDbContext CreateGuardContext() => new(new DbContextOptionsBuilder<SharedPersistenceDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReceiptJson_RoundTripsSuccessAndFailureWithoutReadingTheWrongResultVariant(bool success) {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new AtomicResultJsonConverter() } };
        var error = new Error("Recipe.NotFound", "Not found", ErrorKind.NotFound);
        Result original = success ? Result.Success() : Result.Failure(error);

        string json = JsonSerializer.Serialize(original, options);
        Result? replayed = JsonSerializer.Deserialize<Result>(json, options);

        Assert.NotNull(replayed);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(success, document.RootElement.GetProperty("success").GetBoolean());
        if (success) {
            ResultAssert.Success(replayed);
            Assert.False(document.RootElement.TryGetProperty("error", out _));
        } else {
            ResultAssert.Failure(replayed, error.Code);
            Assert.Equal(error, replayed.Error);
        }
    }

    [Fact]
    public void FailedValueReceipt_PreservesTheErrorAndDoesNotSerializeAnUnavailableValue() {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new AtomicResultJsonConverter() } };
        var error = new Error("Recipe.NotFound", "Not found", ErrorKind.NotFound,
            new Dictionary<string, string[]>(StringComparer.Ordinal) { ["recipeId"] = ["The recipe is missing."] });

        string json = JsonSerializer.Serialize(Result.Failure<string>(error), options);
        Result<string>? replayed = JsonSerializer.Deserialize<Result<string>>(json, options);

        Assert.NotNull(replayed);
        ResultAssert.Failure(replayed, error.Code);
        Assert.Equal(error, replayed.Error);
        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("value", out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedCommit_WithoutAVerifiableProviderRemainsUnknownEvenIfDisposalFails(bool disposalFails) {
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction, IInfrastructure<DbTransaction>>();
        await using DbTransaction providerTransaction = Substitute.For<DbTransaction>();
        ((IInfrastructure<DbTransaction>)transaction).Instance.Returns(providerTransaction);
        var commitFailure = new IOException("Connection lost while committing.");
        transaction.CommitAsync(CancellationToken.None).Returns(Task.FromException(commitFailure));
        if (disposalFails) {
            transaction.DisposeAsync().Returns(ValueTask.FromException(new IOException("Disposal failed.")));
        }

        CommitOutcomeUnknownException result = await Assert.ThrowsAsync<CommitOutcomeUnknownException>(() =>
            VerifiedTransactionCommit.CommitAsync(transaction, CancellationToken.None));

        Assert.Same(commitFailure, result.InnerException);
        _ = transaction.Received(1).CommitAsync(CancellationToken.None);
        _ = transaction.Received(1).DisposeAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScalarCommands_UseTheSharedSnapshotBudgetAndPreserveSuppressedResults(bool bounded) {
        var interceptor = new ReadSnapshotBudgetInterceptor { MaximumQueries = bounded ? 1 : null };
        await using DbCommand command = Substitute.For<DbCommand>();
        var cached = InterceptionResult<object>.SuppressWithResult("cached");

        InterceptionResult<object> first = await interceptor.ScalarExecutingAsync(command, eventData: null!, cached);

        Assert.Equal("cached", first.Result);
        if (bounded) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => interceptor.ScalarExecutingAsync(command, eventData: null!, cached).AsTask());
        } else {
            InterceptionResult<object> second = await interceptor.ScalarExecutingAsync(command, eventData: null!, cached);
            Assert.Equal("cached", second.Result);
        }
    }
}
