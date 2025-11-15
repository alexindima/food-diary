using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Commands.DuplicateProduct;

public class DuplicateProductCommandHandler(IProductRepository productRepository)
    : ICommandHandler<DuplicateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(DuplicateProductCommand command, CancellationToken cancellationToken)
    {
        var original = await productRepository.GetByIdAsync(
            command.ProductId,
            command.UserId!.Value,
            includePublic: true,
            cancellationToken: cancellationToken);

        if (original is null)
        {
            return Result.Failure<ProductResponse>(Errors.Product.NotFound(command.ProductId.Value));
        }

        var duplicate = Product.Create(
            command.UserId.Value,
            original.Name,
            original.BaseUnit,
            original.BaseAmount,
            original.CaloriesPerBase,
            original.ProteinsPerBase,
            original.FatsPerBase,
            original.CarbsPerBase,
            original.FiberPerBase,
            original.Barcode,
            original.Brand,
            original.ProductType,
            original.Category,
            original.Description,
            original.Comment,
            original.ImageUrl,
            original.Visibility);

        await productRepository.AddAsync(duplicate);

        return Result.Success(duplicate.ToResponse(0, true));
    }
}
