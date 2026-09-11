using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionProcessorTests {
    [Fact]
    public async Task ProcessNextAsync_SavesVisionBeforeNutritionAndUsesDistinctStableQuotaIds() {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        ISender sender = Substitute.For<ISender>();
        FoodRecognitionJobModel job = Job();
        var vision = new FoodVisionModel([new FoodVisionItemModel("Apple", NameLocal: null, 100, "g", 1)]);
        var nutrition = new FoodNutritionModel(52, 0, 0, 14, 2, 0, []);
        store.ClaimAsync(Arg.Any<CancellationToken>()).Returns(job);
        store.SaveVisionAsync(job.Id, vision, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        sender.Send(Arg.Any<AnalyzeFoodImageCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(vision));
        sender.Send(Arg.Any<CalculateFoodNutritionCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(nutrition));
        var processor = new FoodRecognitionProcessor(store, sender);

        Assert.True(await processor.ProcessNextAsync(CancellationToken.None));

        Received.InOrder(() => {
            _ = store.ClaimAsync(Arg.Any<CancellationToken>());
            _ = sender.Send(Arg.Any<AnalyzeFoodImageCommand>(), Arg.Any<CancellationToken>());
            _ = store.SaveVisionAsync(job.Id, vision, Arg.Any<CancellationToken>());
            _ = sender.Send(Arg.Any<CalculateFoodNutritionCommand>(), Arg.Any<CancellationToken>());
            _ = store.CompleteAsync(job.Id, nutrition, errorCode: null, nutritionErrorCode: null, Arg.Any<CancellationToken>());
        });
        var first = (AnalyzeFoodImageCommand)sender.ReceivedCalls().First().GetArguments()[0]!;
        var second = (CalculateFoodNutritionCommand)sender.ReceivedCalls().Last().GetArguments()[0]!;
        Assert.Matches("^[0-9A-F]{64}$", first.RequestId);
        Assert.NotEqual(first.RequestId, second.RequestId, StringComparer.Ordinal);
    }

    [Fact]
    public async Task ProcessNextAsync_ProviderFailureIsTerminalWithoutNutritionOrAutomaticRetry() {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        ISender sender = Substitute.For<ISender>();
        FoodRecognitionJobModel job = Job();
        store.ClaimAsync(Arg.Any<CancellationToken>()).Returns(job, (FoodRecognitionJobModel?)null);
        sender.Send(Arg.Any<AnalyzeFoodImageCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<FoodVisionModel>(AiErrors.OpenAiFailed("timeout")));
        var processor = new FoodRecognitionProcessor(store, sender);

        Assert.True(await processor.ProcessNextAsync(CancellationToken.None));
        Assert.False(await processor.ProcessNextAsync(CancellationToken.None));

        await store.Received(1).CompleteAsync(job.Id, nutrition: null, "Ai.OpenAiFailed", nutritionErrorCode: null, Arg.Any<CancellationToken>());
        await sender.Received(1).Send(Arg.Any<AnalyzeFoodImageCommand>(), Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<CalculateFoodNutritionCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessNextAsync_LostClaimNeverStartsSecondPaidStage() {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        ISender sender = Substitute.For<ISender>();
        FoodRecognitionJobModel job = Job();
        store.ClaimAsync(Arg.Any<CancellationToken>()).Returns(job);
        sender.Send(Arg.Any<AnalyzeFoodImageCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new FoodVisionModel([])));
        store.SaveVisionAsync(job.Id, Arg.Any<FoodVisionModel>(), Arg.Any<CancellationToken>()).Returns(returnThis: false);

        await new FoodRecognitionProcessor(store, sender).ProcessNextAsync(CancellationToken.None);

        await sender.DidNotReceive().Send(Arg.Any<CalculateFoodNutritionCommand>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().CompleteAsync(Arg.Any<Guid>(), Arg.Any<FoodNutritionModel?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessNextAsync_NutritionFailurePreservesVisionAndDoesNotRepeatIt() {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        ISender sender = Substitute.For<ISender>();
        FoodRecognitionJobModel job = Job();
        var vision = new FoodVisionModel([new FoodVisionItemModel("Apple", NameLocal: null, 100, "g", 1)]);
        store.ClaimAsync(Arg.Any<CancellationToken>()).Returns(job);
        store.SaveVisionAsync(job.Id, vision, Arg.Any<CancellationToken>()).Returns(returnThis: true);
        sender.Send(Arg.Any<AnalyzeFoodImageCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(vision));
        sender.Send(Arg.Any<CalculateFoodNutritionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<FoodNutritionModel>(AiErrors.QuotaExceeded()));

        await new FoodRecognitionProcessor(store, sender).ProcessNextAsync(CancellationToken.None);

        await store.Received(1).CompleteAsync(job.Id, nutrition: null, errorCode: null, "Ai.QuotaExceeded", Arg.Any<CancellationToken>());
    }

    private static FoodRecognitionJobModel Job() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "https://example.com/image", Description: null, "Running", DateTime.UtcNow, DateTime.UtcNow);
}
