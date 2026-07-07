using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DuplicateProduct;

public sealed class DuplicateProductCommandHandler(
    IProductReadRepository productReadRepository,
    IProductWriteRepository productWriteRepository)
    : ICommandHandler<DuplicateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>> Handle(DuplicateProductCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<ProductModel>(userIdResult);
        }

        Result<ProductId> productIdResult = RequiredIdParser.Parse(
            command.ProductId,
            nameof(command.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ProductModel, ProductId>(productIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductId productId = productIdResult.Value;

        Product? original = await productReadRepository.GetByIdAsync(
            productId,
            userId,
            includePublic: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (original is null) {
            return Result.Failure<ProductModel>(Errors.Product.NotFound(command.ProductId));
        }

        var duplicate = Product.Create(
            userId,
            original.Name,
            original.BaseUnit,
            original.BaseAmount,
            original.DefaultPortionAmount,
            original.CaloriesPerBase,
            original.ProteinsPerBase,
            original.FatsPerBase,
            original.CarbsPerBase,
            original.FiberPerBase,
            original.AlcoholPerBase,
            original.Barcode,
            original.Brand,
            original.ProductType,
            original.Category,
            original.Description,
            original.Comment,
            original.ImageUrl,
            imageAssetId: null,
            original.Visibility);

        await productWriteRepository.AddAsync(duplicate, cancellationToken).ConfigureAwait(false);

        return Result.Success(duplicate.ToModel(isOwnedByCurrentUser: true));
    }
}
