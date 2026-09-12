using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Meals.Commands.CreateMeal;
using FoodDiary.Application.Meals.Commands.CreateMealFromRecognition;
using FoodDiary.Application.Meals.Mappings;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Meals;

[ExcludeFromCodeCoverage]
public sealed class CreateMealFromRecognitionTests {
    [Fact]
    public async Task CompletedRecognition_UsesExistingCreateHandlerAndPersistsReceipt() {
        var owner = new UserId(Guid.NewGuid());
        DateTime now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        FoodRecognitionJobModel job = CreateJob(owner, now);
        var meal = Meal.Create(owner, now);
        IMealRecognitionReceiptRepository receipts = Substitute.For<IMealRecognitionReceiptRepository>();
        IFoodRecognitionResultReader reader = Substitute.For<IFoodRecognitionResultReader>();
        reader.GetCompletedAsync(owner.Value, job.Id, Arg.Any<CancellationToken>()).Returns(Result.Success(job));
        FoodDiary.Mediator.IRequestHandler<CreateMealCommand, Result<MealModel>> create = Substitute.For<FoodDiary.Mediator.IRequestHandler<CreateMealCommand, Result<MealModel>>>();
        create.Handle(Arg.Any<CreateMealCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(meal.ToModel()));
        var handler = new CreateMealFromRecognitionCommandHandler(new InlineTransactions(), receipts, reader, create,
            Substitute.For<ICurrentUserAccessService>(), TimeProvider.System);

        Result<RecognizedMealCreationModel> result = await handler.Handle(new CreateMealFromRecognitionCommand(owner.Value, job.Id, now), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(meal.Id.Value, result.Value.MealId);
        Assert.Equal(job.Id, result.Value.OperationId);
        await create.Received(1).Handle(Arg.Is<CreateMealCommand>(command => command.UserId == owner.Value && command.Date == now &&
            command.ImageAssetId == job.ImageAssetId && command.IsNutritionAutoCalculated && command.AiSessions.Count == 1 &&
            command.AiSessions[0].Items[0].Calories == 52 && command.AiSessions[0].Items[0].Amount == 100), Arg.Any<CancellationToken>());
        await receipts.Received(1).AddAsync(Arg.Is<MealRecognitionReceipt>(receipt => receipt.MealId == meal.Id &&
            receipt.MealVersion == 7 && receipt.RecognitionId == job.Id), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Replay_DoesNotNeedRetainedAiJobOrRecreateDeletedMeal(bool undone, bool deleted) {
        var owner = new UserId(Guid.NewGuid());
        DateTime now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var receipt = MealRecognitionReceipt.Create(id, owner, id, new MealId(Guid.NewGuid()), 7, now, now, TimeSpan.FromHours(24));
        if (undone) {
            receipt.TryUndo(currentMealVersion: null, now);
        }
        IMealRecognitionReceiptRepository receipts = Substitute.For<IMealRecognitionReceiptRepository>();
        receipts.FindByRecognitionAsync(owner, id, Arg.Any<CancellationToken>()).Returns(receipt);
        if (!deleted) {
            receipts.LockMealForUndoAsync(owner, receipt.MealId, Arg.Any<CancellationToken>()).Returns((Meal.Create(owner, now), 8u));
        }
        IFoodRecognitionResultReader reader = Substitute.For<IFoodRecognitionResultReader>();
        FoodDiary.Mediator.IRequestHandler<CreateMealCommand, Result<MealModel>> create = Substitute.For<FoodDiary.Mediator.IRequestHandler<CreateMealCommand, Result<MealModel>>>();
        var handler = new CreateMealFromRecognitionCommandHandler(new InlineTransactions(), receipts, reader, create,
            Substitute.For<ICurrentUserAccessService>(), TimeProvider.System);

        Result<RecognizedMealCreationModel> result = await handler.Handle(new CreateMealFromRecognitionCommand(owner.Value, id, now.AddTicks(3)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(receipt.MealId.Value, result.Value.MealId);
        Assert.Equal(undone || deleted, result.Value.Undone);
        Assert.Equal(undone || deleted, receipt.UndoneAtUtc.HasValue);
        await reader.DidNotReceive().GetCompletedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await create.DidNotReceive().Handle(Arg.Any<CreateMealCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReusingRecognitionWithDifferentTime_ReturnsConflict() {
        var owner = new UserId(Guid.NewGuid());
        DateTime now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var receipt = MealRecognitionReceipt.Create(id, owner, id, new MealId(Guid.NewGuid()), 7, now, now, TimeSpan.FromHours(24));
        IMealRecognitionReceiptRepository receipts = Substitute.For<IMealRecognitionReceiptRepository>();
        receipts.FindByRecognitionAsync(owner, id, Arg.Any<CancellationToken>()).Returns(receipt);
        IFoodRecognitionResultReader reader = Substitute.For<IFoodRecognitionResultReader>();
        var handler = new CreateMealFromRecognitionCommandHandler(new InlineTransactions(), receipts, reader,
            Substitute.For<FoodDiary.Mediator.IRequestHandler<CreateMealCommand, Result<MealModel>>>(), Substitute.For<ICurrentUserAccessService>(), TimeProvider.System);

        Result<RecognizedMealCreationModel> result = await handler.Handle(new CreateMealFromRecognitionCommand(owner.Value, id, now.AddMinutes(1)), CancellationToken.None);

        Assert.Equal("Meal.RecognitionConflict", result.Error.Code);
        await reader.DidNotReceive().GetCompletedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidOrUnavailableRecognition_DoesNotCreateMeal(bool unavailable) {
        var owner = new UserId(Guid.NewGuid());
        DateTime now = DateTime.UtcNow;
        FoodRecognitionJobModel job = CreateJob(owner, now);
        IFoodRecognitionResultReader reader = Substitute.For<IFoodRecognitionResultReader>();
        reader.GetCompletedAsync(owner.Value, job.Id, Arg.Any<CancellationToken>()).Returns(unavailable
            ? Result.Failure<FoodRecognitionJobModel>(new Error("Ai.RecognitionNotReady", "Not ready.", ErrorKind.Conflict))
            : Result.Success(job with { Nutrition = job.Nutrition! with { Items = [new FoodNutritionItemModel("Apple", -1, "g", 52, 0, 0, 14, 2, 0)] } }));
        FoodDiary.Mediator.IRequestHandler<CreateMealCommand, Result<MealModel>> create = Substitute.For<FoodDiary.Mediator.IRequestHandler<CreateMealCommand, Result<MealModel>>>();
        var handler = new CreateMealFromRecognitionCommandHandler(new InlineTransactions(), Substitute.For<IMealRecognitionReceiptRepository>(), reader,
            create, Substitute.For<ICurrentUserAccessService>(), TimeProvider.System);

        Result<RecognizedMealCreationModel> result = await handler.Handle(new CreateMealFromRecognitionCommand(owner.Value, job.Id, now), CancellationToken.None);

        Assert.True(result.IsFailure);
        await create.DidNotReceive().Handle(Arg.Any<CreateMealCommand>(), Arg.Any<CancellationToken>());
    }

    private static FoodRecognitionJobModel CreateJob(UserId owner, DateTime now) => new(Guid.NewGuid(), owner.Value, Guid.NewGuid(),
        "https://images.example.com/apple.jpg", "Apple", "Succeeded", now, now,
        new FoodVisionModel([new FoodVisionItemModel("Apple", "Яблоко", 100, "g", 0.9m)]),
        new FoodNutritionModel(52, 0, 0, 14, 2, 0, [new FoodNutritionItemModel("Apple", 100, "g", 52, 0, 0, 14, 2, 0)]));

    [ExcludeFromCodeCoverage]
    private sealed class InlineTransactions : IMealRecognitionTransactionRunner {
        public Task<T> ExecuteSerializedAsync<T>(UserId userId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        public Task<uint> FlushCreatedMealAsync(MealId mealId, UserId userId, CancellationToken cancellationToken = default) => Task.FromResult(7u);
    }
}
