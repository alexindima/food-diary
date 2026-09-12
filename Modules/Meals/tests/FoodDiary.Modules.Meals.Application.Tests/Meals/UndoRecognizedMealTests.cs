using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Meals.Commands.UndoRecognizedMeal;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Meals;

[ExcludeFromCodeCoverage]
public sealed class UndoRecognizedMealTests {
    [Theory]
    [InlineData(7, 0, true)]
    [InlineData(8, 0, false)]
    [InlineData(7, 25, false)]
    public async Task Undo_DeletesOnlyUnchangedMealWithinDeadline(uint currentVersion, int elapsedHours, bool shouldDelete) {
        DateTime now = DateTime.UtcNow;
        var owner = new UserId(Guid.NewGuid());
        var meal = Meal.Create(owner, now);
        var receipt = MealRecognitionReceipt.Create(Guid.NewGuid(), owner, Guid.NewGuid(), meal.Id, 7,
            now, now.AddHours(-elapsedHours), TimeSpan.FromHours(24));
        IMealRecognitionReceiptRepository receipts = Substitute.For<IMealRecognitionReceiptRepository>();
        receipts.FindAsync(owner, receipt.OperationId, Arg.Any<CancellationToken>()).Returns(receipt);
        receipts.LockMealForUndoAsync(owner, meal.Id, Arg.Any<CancellationToken>()).Returns((meal, currentVersion));
        IMealWriteRepository meals = Substitute.For<IMealWriteRepository>();
        var handler = new UndoRecognizedMealCommandHandler(new InlineTransactions(), receipts, meals,
            Substitute.For<ICurrentUserAccessService>(), TimeProvider.System);

        Result<RecognizedMealUndoModel> result = await handler.Handle(new UndoRecognizedMealCommand(owner.Value, receipt.OperationId), CancellationToken.None);

        Assert.Equal(shouldDelete, result.IsSuccess);
        await meals.Received(shouldDelete ? 1 : 0).DeleteAsync(meal, Arg.Any<CancellationToken>());
        if (shouldDelete) {
            Assert.NotNull(receipt.UndoneAtUtc);
            Result<RecognizedMealUndoModel> replay = await handler.Handle(new UndoRecognizedMealCommand(owner.Value, receipt.OperationId), CancellationToken.None);
            Assert.Equal("AlreadyUndone", replay.Value.Status);
            await meals.Received(1).DeleteAsync(meal, Arg.Any<CancellationToken>());
        } else {
            Assert.Null(receipt.UndoneAtUtc);
            Assert.Equal(currentVersion == 8 ? "Meal.RecognitionUndoChanged" : "Meal.RecognitionUndoExpired", result.Error.Code);
        }
    }

    [Fact]
    public async Task DeletedMeal_CompletesReceiptWithoutDeletingAnythingElse() {
        DateTime now = DateTime.UtcNow;
        var owner = new UserId(Guid.NewGuid());
        var receipt = MealRecognitionReceipt.Create(Guid.NewGuid(), owner, Guid.NewGuid(), new MealId(Guid.NewGuid()), 7,
            now, now, TimeSpan.FromHours(24));
        IMealRecognitionReceiptRepository receipts = Substitute.For<IMealRecognitionReceiptRepository>();
        receipts.FindAsync(owner, receipt.OperationId, Arg.Any<CancellationToken>()).Returns(receipt);
        IMealWriteRepository meals = Substitute.For<IMealWriteRepository>();
        var handler = new UndoRecognizedMealCommandHandler(new InlineTransactions(), receipts, meals,
            Substitute.For<ICurrentUserAccessService>(), TimeProvider.System);

        Result<RecognizedMealUndoModel> result = await handler.Handle(new UndoRecognizedMealCommand(owner.Value, receipt.OperationId), CancellationToken.None);

        Assert.Equal("AlreadyDeleted", result.Value.Status);
        Assert.NotNull(receipt.UndoneAtUtc);
        await meals.DidNotReceive().DeleteAsync(Arg.Any<Meal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AccessRevokedWhileWaiting_DoesNotReadOrMutateReceipt() {
        var owner = new UserId(Guid.NewGuid());
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(owner, Arg.Any<CancellationToken>()).Returns((Error?)null,
            new Error("User.AccountDeleted", "Account unavailable.", ErrorKind.Forbidden));
        IMealRecognitionReceiptRepository receipts = Substitute.For<IMealRecognitionReceiptRepository>();
        IMealWriteRepository meals = Substitute.For<IMealWriteRepository>();
        var handler = new UndoRecognizedMealCommandHandler(new InlineTransactions(), receipts, meals, access, TimeProvider.System);

        Result<RecognizedMealUndoModel> result = await handler.Handle(new UndoRecognizedMealCommand(owner.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal("User.AccountDeleted", result.Error.Code);
        await receipts.DidNotReceive().FindAsync(Arg.Any<UserId>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await meals.DidNotReceive().DeleteAsync(Arg.Any<Meal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForeignReceipt_IsNotExposedOrUsedForDeletion() {
        var owner = new UserId(Guid.NewGuid());
        DateTime now = DateTime.UtcNow;
        var receipt = MealRecognitionReceipt.Create(Guid.NewGuid(), new UserId(Guid.NewGuid()), Guid.NewGuid(), new MealId(Guid.NewGuid()),
            7, now, now, TimeSpan.FromHours(24));
        IMealRecognitionReceiptRepository receipts = Substitute.For<IMealRecognitionReceiptRepository>();
        receipts.FindAsync(owner, receipt.OperationId, Arg.Any<CancellationToken>()).Returns(receipt);
        IMealWriteRepository meals = Substitute.For<IMealWriteRepository>();
        var handler = new UndoRecognizedMealCommandHandler(new InlineTransactions(), receipts, meals,
            Substitute.For<ICurrentUserAccessService>(), TimeProvider.System);

        Result<RecognizedMealUndoModel> result = await handler.Handle(new UndoRecognizedMealCommand(owner.Value, receipt.OperationId), CancellationToken.None);

        Assert.Equal("Meal.RecognitionOperationNotFound", result.Error.Code);
        await receipts.DidNotReceive().LockMealForUndoAsync(Arg.Any<UserId>(), Arg.Any<MealId>(), Arg.Any<CancellationToken>());
        await meals.DidNotReceive().DeleteAsync(Arg.Any<Meal>(), Arg.Any<CancellationToken>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class InlineTransactions : IMealRecognitionTransactionRunner {
        public Task<T> ExecuteSerializedAsync<T>(UserId userId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        public Task<uint> FlushCreatedMealAsync(MealId mealId, UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
