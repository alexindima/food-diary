using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Application.Ai.Commands.StartFoodRecognition;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class StartFoodRecognitionCommandHandlerTests {
    [Theory]
    [InlineData(false, false, "Ai.ConsentRequired")]
    [InlineData(true, false, "Ai.Forbidden")]
    public async Task Handle_DeniesUnconsentedOrForeignImageBeforeEnqueue(bool consent, bool owned, string expectedError) {
        IFoodRecognitionJobStore store = Substitute.For<IFoodRecognitionJobStore>();
        IImageAssetAccessService images = Substitute.For<IImageAssetAccessService>();
        IAiUserContextService users = Substitute.For<IAiUserContextService>();
        var userId = new UserId(Guid.NewGuid());
        var imageId = new ImageAssetId(Guid.NewGuid());
        users.GetAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Success(new AiUserContext(userId, "en", 1000, 1000, consent)));
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
