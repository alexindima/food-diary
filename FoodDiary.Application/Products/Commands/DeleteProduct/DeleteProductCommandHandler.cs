using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Products;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler(IProductRepository productRepository)
    : ICommandHandler<DeleteProductCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(
            command.ProductId,
            command.UserId!.Value,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null)
        {
            return Result.Failure<bool>(Errors.Product.NotAccessible(command.ProductId.Value));
        }

        await productRepository.DeleteAsync(product);
        return Result.Success(true);
    }
}

