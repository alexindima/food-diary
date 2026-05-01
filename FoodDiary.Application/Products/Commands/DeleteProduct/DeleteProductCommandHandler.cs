using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

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
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var productId = new ProductId(command.ProductId);

        var product = await productRepository.GetByIdForUpdateAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        var assetId = product.ImageAssetId;
        await productRepository.DeleteAsync(product, cancellationToken);

        if (assetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId.Value, cancellationToken);
        }

        return Result.Success();
    }
}
