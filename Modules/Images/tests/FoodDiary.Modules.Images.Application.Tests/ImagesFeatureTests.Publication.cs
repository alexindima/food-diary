using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Application.Queries.GetOwnedImageContent;
using FoodDiary.Modules.Images.Application.Services;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Application.Tests;

public partial class ImagesFeatureTests {
    [Theory]
    [InlineData(true, "UserId")]
    [InlineData(false, "AssetId")]
    public async Task GetOwnedImageContent_RejectsEmptyIdentifierBeforeContentAccessAsync(bool missingOwner, string expectedField) {
        IImageAssetContentService content = Substitute.For<IImageAssetContentService>();
        var handler = new GetOwnedImageContentQueryHandler(content);
        var query = new GetOwnedImageContentQuery(
            missingOwner ? Guid.Empty : Guid.NewGuid(),
            missingOwner ? Guid.NewGuid() : Guid.Empty);

        Result<string> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Image.InvalidData", result.Error.Code),
            () => Assert.Contains(expectedField, result.Error.Message, StringComparison.Ordinal));
        Assert.Empty(content.ReceivedCalls());
    }

    [Theory]
    [InlineData(false, false, "Image.InvalidData")]
    [InlineData(true, false, "Image.StorageError")]
    [InlineData(true, true, "Image.StorageError")]
    public async Task PrivatePromotion_KeepsAssetAndDoesNotScheduleDeletionAfterFailureAsync(
        bool confirmed, bool storageThrows, string expectedCode) {
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/recipe.jpg", "https://cdn.example/recipe.jpg", keepPrivate: true);
        if (confirmed) {
            asset.Confirm();
        }
        var repository = new FakeImageAssetRepository();
        await repository.AddAsync(asset, CancellationToken.None);
        IImageStorageService storage = Substitute.For<IImageStorageService>();
        var key = ObjectStorageKey.FromStoredValue(asset.ObjectKey);
        storage.PromotePrivateImageAsync(key, Arg.Any<CancellationToken>()).Returns(_ => storageThrows
            ? Task.FromException<ImageObjectValidationResult>(new IOException("Storage is unavailable."))
            : Task.FromResult<ImageObjectValidationResult>(new ImageObjectValidationResult.Invalid("missing", "Missing object")));
        var outbox = new FakeImageObjectDeletionOutbox();
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var service = new RecipeImagePromotionService(repository, storage, new InlineImageTransactionRunner(), outbox, unitOfWork);

        Result result = await service.PromoteAsync(asset.Id, owner, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal(expectedCode, result.Error.Code),
            () => Assert.True(asset.IsPrivate),
            () => Assert.Empty(outbox.Deletions));
        await storage.Received(confirmed ? 1 : 0).PromotePrivateImageAsync(key, Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PrivatePromotion_PropagatesCallerCancellationWithoutSavingOrSchedulingDeletionAsync() {
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/recipe.jpg", "https://cdn.example/recipe.jpg", keepPrivate: true);
        asset.Confirm();
        var repository = new FakeImageAssetRepository();
        await repository.AddAsync(asset, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        IImageStorageService storage = Substitute.For<IImageStorageService>();
        storage.PromotePrivateImageAsync(ObjectStorageKey.FromStoredValue(asset.ObjectKey), Arg.Any<CancellationToken>())
            .Returns(async call => {
                await cancellation.CancelAsync();
                return await Task.FromCanceled<ImageObjectValidationResult>(call.Arg<CancellationToken>());
            });
        var outbox = new FakeImageObjectDeletionOutbox();
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var service = new RecipeImagePromotionService(repository, storage, new InlineImageTransactionRunner(), outbox, unitOfWork);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.PromoteAsync(asset.Id, owner, cancellation.Token));

        Assert.Multiple(() => Assert.True(asset.IsPrivate), () => Assert.Empty(outbox.Deletions));
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
