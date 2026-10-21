using System.Data.Common;
using System.Text.Json;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.Tests.Persistence.Shared;

[ExcludeFromCodeCoverage]
public sealed class AtomicReceiptRuntimeTests {
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
