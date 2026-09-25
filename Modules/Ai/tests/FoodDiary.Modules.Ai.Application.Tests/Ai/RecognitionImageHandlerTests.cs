using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Application.Commands.AnalyzeFoodImage;
using FoodDiary.Modules.Ai.Application.Commands.DeleteFoodRecognition;
using FoodDiary.Modules.Ai.Application.Commands.StartFoodRecognition;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class RecognitionImageHandlerTests {
    [Fact]
    public async Task Start_DuplicatePrimaryImageIsRejectedBeforeReadingUserOrEnqueueing() {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        IImageAssetAccessService images = Substitute.For<IImageAssetAccessService>();
        IUserAiProfileReadService users = Substitute.For<IUserAiProfileReadService>();
        var primary = Guid.NewGuid();
        var command = new StartFoodRecognitionCommand(Guid.NewGuid(), Guid.NewGuid(), primary, Description: null, IsProductLabel: true, [primary]);

        Result<FoodRecognitionJobModel> result = await new StartFoodRecognitionCommandHandler(store, images, users, TimeProvider.System)
            .Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Validation.Invalid", result.Error.Code),
            () => Assert.Empty(users.ReceivedCalls()),
            () => Assert.Empty(images.ReceivedCalls()),
            () => Assert.Empty(store.ReceivedCalls()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Analyze_AdditionalImagesAreResolvedInOrderOrStopBeforeProvider(bool denied) {
        var owner = UserId.New();
        var primary = ImageAssetId.New();
        var extra = ImageAssetId.New();
        using var cancellation = new CancellationTokenSource();
        IImageAssetContentService images = Substitute.For<IImageAssetContentService>();
        IOpenAiFoodService provider = Substitute.For<IOpenAiFoodService>();
        images.GetDataUrlAsync(primary, owner, cancellation.Token).Returns(Result.Success("primary-data"));
        Error error = AiErrors.Forbidden();
        images.GetDataUrlAsync(extra, owner, cancellation.Token).Returns(denied ? Result.Failure<string>(error) : Result.Success("extra-data"));
        var vision = new FoodVisionModel([]);
        provider.AnalyzeFoodImageAsync("primary-data", owner, "label", "request", cancellation.Token, product: Arg.Any<ProductImageAnalysis>()).Returns(Result.Success(vision));

        Result<FoodVisionModel> result = await new AnalyzeFoodImageCommandHandler(images, provider).Handle(
            new AnalyzeFoodImageCommand(owner.Value, primary.Value, "label", "request", IsProductLabel: true, [extra.Value]), cancellation.Token);

        if (denied) {
            ResultAssert.Failure(result);
            Assert.Equal(error, result.Error);
            Assert.Empty(provider.ReceivedCalls());
        } else {
            ResultAssert.Success(result);
            Assert.Same(vision, result.Value);
            await provider.Received(1).AnalyzeFoodImageAsync("primary-data", owner, "label", "request", cancellation.Token,
                product: Arg.Is<ProductImageAnalysis>(product => product.AdditionalImageUrls.SequenceEqual(new[] { "extra-data" }, StringComparer.Ordinal)));
        }
        Received.InOrder(() => {
            _ = images.GetDataUrlAsync(primary, owner, cancellation.Token);
            _ = images.GetDataUrlAsync(extra, owner, cancellation.Token);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_AdditionalImagesAreStoredOrMissingImagePreventsEnqueue(bool missing) {
        var owner = UserId.New();
        var primary = ImageAssetId.New();
        var extra = ImageAssetId.New();
        using var cancellation = new CancellationTokenSource();
        IImageAssetAccessService images = Substitute.For<IImageAssetAccessService>();
        IUserAiProfileReadService users = Substitute.For<IUserAiProfileReadService>();
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        users.GetAiProfileAsync(owner, cancellation.Token).Returns(Result.Success(new UserAiProfileModel(owner, "en", 1000, 1000, HasAcceptedAiConsent: true)));
        images.ResolveOptionalAsync(primary, owner, cancellation.Token).Returns(Result.Success<ImageAssetReadModel?>(new ImageAssetReadModel(primary, "primary-url")));
        images.ResolveOptionalAsync(extra, owner, cancellation.Token).Returns(Result.Success<ImageAssetReadModel?>(missing ? null : new ImageAssetReadModel(extra, "extra-url")));
        store.CreateAsync(Arg.Any<FoodRecognitionJobModel>(), cancellation.Token).Returns(call => Result.Success(call.Arg<FoodRecognitionJobModel>()));
        var command = new StartFoodRecognitionCommand(owner.Value, Guid.NewGuid(), primary.Value, "label", IsProductLabel: true, [extra.Value]);

        Result<FoodRecognitionJobModel> result = await new StartFoodRecognitionCommandHandler(store, images, users, TimeProvider.System).Handle(command, cancellation.Token);

        if (missing) {
            ResultAssert.Failure(result);
            Assert.Equal("Ai.ImageNotFound", result.Error.Code);
            Assert.Empty(store.ReceivedCalls());
        } else {
            ResultAssert.Success(result);
            Assert.Multiple(
                () => Assert.Equal(command.Id, result.Value.Id),
                () => Assert.Equal(owner.Value, result.Value.UserId),
                () => Assert.True(result.Value.IsProductLabel),
                () => Assert.Equal(new FoodRecognitionImageModel(extra.Value, "extra-url"), Assert.Single(result.Value.AdditionalImages!)));
            await store.Received(1).CreateAsync(result.Value, cancellation.Token);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_PassesOwnerIdAndCancellationAndPreservesStoreResult(bool failure) {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        var command = new DeleteFoodRecognitionCommand(Guid.NewGuid(), Guid.NewGuid());
        using var cancellation = new CancellationTokenSource();
        Result expected = failure ? Result.Failure(AiErrors.Forbidden()) : Result.Success();
        store.DeleteCompletedAsync(command.UserId, command.Id, cancellation.Token).Returns(expected);

        Result actual = await new DeleteFoodRecognitionCommandHandler(store).Handle(command, cancellation.Token);

        Assert.Same(expected, actual);
        await store.Received(1).DeleteCompletedAsync(command.UserId, command.Id, cancellation.Token);
    }
}
