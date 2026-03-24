using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductByIdQuery, Result<ProductModel>> {
    public async Task<Result<ProductModel>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<ProductModel>(Errors.Authentication.InvalidToken);
        }

        var userId = query.UserId.Value;
        var product = await productRepository.GetByIdAsync(
            query.ProductId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure<ProductModel>(Errors.Product.NotAccessible(query.ProductId.Value));
        }

        var usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
        var isOwner = product.UserId == userId;
        return Result.Success(product.ToModel(usageCount, isOwner));
    }
}
