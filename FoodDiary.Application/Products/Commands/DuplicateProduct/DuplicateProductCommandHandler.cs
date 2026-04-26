using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DuplicateProduct;

public class DuplicateProductCommandHandler(IProductRepository productRepository)
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

        var original = await productRepository.GetByIdAsync(
            productId,
            userId,
            includePublic: true,
            cancellationToken: cancellationToken);

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
            null,
            original.Visibility);

        await productRepository.AddAsync(duplicate, cancellationToken);

        return Result.Success(duplicate.ToModel(0, true));
    }
}
