using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(
    IProductRepository productRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetProductByIdQuery, Result<ProductModel>> {
    public async Task<Result<ProductModel>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Authentication.InvalidToken);
        }

        if (query.ProductId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Validation.Invalid(nameof(query.ProductId), "Product id must not be empty."));
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<ProductModel>(accessError);
        }
        var productId = new ProductId(query.ProductId);
        var product = await productRepository.GetByIdAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure<ProductModel>(Errors.Product.NotAccessible(query.ProductId));
        }

        var usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
        var isOwner = product.UserId == userId;
        return Result.Success(product.ToModel(usageCount, isOwner));
    }
}
