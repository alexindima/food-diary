using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Products;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductByIdQuery, Result<ProductResponse>> {
    public async Task<Result<ProductResponse>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken) {
        var userId = query.UserId!.Value;
        var product = await productRepository.GetByIdAsync(
            query.ProductId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure<ProductResponse>(Errors.Product.NotAccessible(query.ProductId.Value));
        }

        var usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
        var isOwner = product.UserId == userId;
        return Result.Success(product.ToResponse(usageCount, isOwner));
    }
}
