using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Modules.Ai.Application.Commands.StartFoodRecognition;

using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class StartFoodRecognitionCommandHandlerTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_MissingUserOrImageNeverEnqueues(bool missingUser) {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        IImageAssetAccessService images = Substitute.For<IImageAssetAccessService>();
        IUserAiProfileReadService users = Substitute.For<IUserAiProfileReadService>();
        var owner = new UserId(Guid.NewGuid());
        users.GetAiProfileAsync(owner, Arg.Any<CancellationToken>()).Returns(missingUser
            ? Result.Failure<UserAiProfileModel>(new Error("User.Missing", "Missing"))
            : Result.Success(new UserAiProfileModel(owner, "en", 1000, 1000, HasAcceptedAiConsent: true)));
        images.ResolveOptionalAsync(Arg.Any<ImageAssetId?>(), owner, Arg.Any<CancellationToken>())
            .Returns(Result.Success<ImageAssetReadModel?>(value: null));

        Result<FoodRecognitionJobModel> result = await new StartFoodRecognitionCommandHandler(store, images, users, TimeProvider.System)
            .Handle(new StartFoodRecognitionCommand(owner.Value, Guid.NewGuid(), Guid.NewGuid(), Description: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(missingUser ? "User.Missing" : "Ai.ImageNotFound", result.Error.Code);
        await store.DidNotReceive().CreateAsync(Arg.Any<FoodRecognitionJobModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_UsesTheRequestedOwnerAndCancellationToken() {
        IFoodRecognitionJobReader store = Substitute.For<IFoodRecognitionJobReader>();
        var owner = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        IReadOnlyList<FoodRecognitionJobModel> jobs = [new(Guid.NewGuid(), owner, Guid.NewGuid(), "https://example.com/image", Description: null, "Queued", DateTime.UtcNow, DateTime.UtcNow)];
        store.ListAsync(owner, cancellation.Token).Returns(jobs);

        Result<IReadOnlyList<FoodRecognitionJobModel>> result = await new FoodDiary.Modules.Ai.Application.Queries.ListFoodRecognitions.ListFoodRecognitionsQueryHandler(store)
            .Handle(new FoodDiary.Modules.Ai.Application.Queries.ListFoodRecognitions.ListFoodRecognitionsQuery(owner), cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Same(jobs, result.Value);
        await store.Received(1).ListAsync(owner, cancellation.Token);
    }

    [Theory]
    [InlineData(false, false, "Ai.ConsentRequired")]
    [InlineData(true, false, "Ai.Forbidden")]
    public async Task Handle_DeniesUnconsentedOrForeignImageBeforeEnqueue(bool consent, bool owned, string expectedError) {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        IImageAssetAccessService images = Substitute.For<IImageAssetAccessService>();
        IUserAiProfileReadService users = Substitute.For<IUserAiProfileReadService>();
        var userId = new UserId(Guid.NewGuid());
        var imageId = new ImageAssetId(Guid.NewGuid());
        users.GetAiProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Success(new UserAiProfileModel(userId, "en", 1000, 1000, consent)));
        images.ResolveOptionalAsync(imageId, userId, Arg.Any<CancellationToken>()).Returns(
            owned ? Result.Success<ImageAssetReadModel?>(new ImageAssetReadModel(imageId, "https://example.com/image"))
                : Result.Failure<ImageAssetReadModel?>(AiErrors.Forbidden()));
        var handler = new StartFoodRecognitionCommandHandler(store, images, users, TimeProvider.System);

        Result<FoodRecognitionJobModel> result = await handler.Handle(
            new StartFoodRecognitionCommand(userId.Value, Guid.NewGuid(), imageId.Value, Description: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error.Code);
        await store.DidNotReceive().CreateAsync(Arg.Any<FoodRecognitionJobModel>(), Arg.Any<CancellationToken>());
    }
}
