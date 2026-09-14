using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionResultReaderTests {
    [Fact]
    public async Task GetCompletedAsync_DoesNotExposeAnotherUsersResult() {
        IFoodRecognitionJobReader jobs = Substitute.For<IFoodRecognitionJobReader>();
        FoodRecognitionJobModel job = CompleteJob();
        var caller = Guid.NewGuid();
        jobs.GetAsync(caller, job.Id, Arg.Any<CancellationToken>()).Returns(job);
        Result<FoodRecognitionJobModel> result = await new FoodRecognitionResultReader(jobs).GetCompletedAsync(caller, job.Id, CancellationToken.None);
        Assert.Equal("Ai.RecognitionNotFound", result.Error.Code);
    }

    [Theory]
    [InlineData("Queued", null, "Ai.RecognitionNotReady")]
    [InlineData("Running", null, "Ai.RecognitionNotReady")]
    [InlineData("Failed", null, "Ai.RecognitionNotUsable")]
    [InlineData("Succeeded", "partial-nutrition", "Ai.RecognitionNotUsable")]
    public async Task GetCompletedAsync_RejectsIncompleteResults(string status, string? nutritionError, string expectedError) {
        IFoodRecognitionJobReader jobs = Substitute.For<IFoodRecognitionJobReader>();
        FoodRecognitionJobModel job = CompleteJob() with { Status = status, NutritionErrorCode = nutritionError };
        jobs.GetAsync(job.UserId, job.Id, Arg.Any<CancellationToken>()).Returns(job);
        Result<FoodRecognitionJobModel> result = await new FoodRecognitionResultReader(jobs).GetCompletedAsync(job.UserId, job.Id, CancellationToken.None);
        Assert.Equal(expectedError, result.Error.Code);
    }

    [Fact]
    public async Task GetCompletedAsync_RejectsEmptyFoodResult() {
        IFoodRecognitionJobReader jobs = Substitute.For<IFoodRecognitionJobReader>();
        FoodRecognitionJobModel job = CompleteJob() with { Vision = new FoodVisionModel([]) };
        jobs.GetAsync(job.UserId, job.Id, Arg.Any<CancellationToken>()).Returns(job);
        Assert.True((await new FoodRecognitionResultReader(jobs).GetCompletedAsync(job.UserId, job.Id, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task GetCompletedAsync_ReturnsOwnedCompleteResult() {
        IFoodRecognitionJobReader jobs = Substitute.For<IFoodRecognitionJobReader>();
        FoodRecognitionJobModel job = CompleteJob();
        jobs.GetAsync(job.UserId, job.Id, Arg.Any<CancellationToken>()).Returns(job);
        Result<FoodRecognitionJobModel> result = await new FoodRecognitionResultReader(jobs).GetCompletedAsync(job.UserId, job.Id, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(job, result.Value);
    }

    [Fact]
    public async Task GetCompletedAsync_RejectsMissingNutritionForRecognizedItem() {
        IFoodRecognitionJobReader jobs = Substitute.For<IFoodRecognitionJobReader>();
        FoodRecognitionJobModel job = CompleteJob() with {
            Vision = new FoodVisionModel([new FoodVisionItemModel("Apple", NameLocal: null, 100, "g", 0.9m),
                new FoodVisionItemModel("Pear", NameLocal: null, 100, "g", 0.9m)]),
        };
        jobs.GetAsync(job.UserId, job.Id, Arg.Any<CancellationToken>()).Returns(job);
        Assert.True((await new FoodRecognitionResultReader(jobs).GetCompletedAsync(job.UserId, job.Id, CancellationToken.None)).IsFailure);
    }

    private static FoodRecognitionJobModel CompleteJob() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "image", Description: null,
        "Succeeded", DateTime.UtcNow, DateTime.UtcNow,
        new FoodVisionModel([new FoodVisionItemModel("Apple", "Яблоко", 100, "g", 0.9m)]),
        new FoodNutritionModel(52, 0.3m, 0.2m, 14, 2.4m, 0, [new FoodNutritionItemModel("Apple", 100, "g", 52, 0.3m, 0.2m, 14, 2.4m, 0)]));
}
