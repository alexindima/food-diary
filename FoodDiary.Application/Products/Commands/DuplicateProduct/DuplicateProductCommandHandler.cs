using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DuplicateProduct;

public class DuplicateProductCommandHandler(
    IProductReadRepository productReadRepository,
    IProductWriteRepository productWriteRepository)
    : ICommandHandler<DuplicateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>> Handle(DuplicateProductCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Authentication.InvalidToken);
        }

        if (command.ProductId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Validation.Invalid(nameof(command.ProductId), "Product id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var productId = new ProductId(command.ProductId);

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
