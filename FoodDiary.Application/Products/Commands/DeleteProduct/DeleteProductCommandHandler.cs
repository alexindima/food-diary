using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    IUserRepository userRepository)
    : ICommandHandler<DeleteProductCommand, Result> {
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.ProductId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.ProductId), "Product id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var productId = new ProductId(command.ProductId);

        Product? product = await productRepository.GetByIdForUpdateAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        ImageAssetId? assetId = product.ImageAssetId;
        await productRepository.DeleteAsync(product, cancellationToken).ConfigureAwait(false);

        if (assetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId.Value, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
