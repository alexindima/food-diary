using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public class AddFavoriteProductCommandHandler(
    IFavoriteProductRepository favoriteProductRepository,
    IProductRepository productRepository,
    IUserRepository userRepository)
    : ICommandHandler<AddFavoriteProductCommand, Result<FavoriteProductModel>> {
    public async Task<Result<FavoriteProductModel>> Handle(
        AddFavoriteProductCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FavoriteProductModel>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<FavoriteProductModel>(accessError);
        }

        var productId = new ProductId(command.ProductId);
        var product = await productRepository.GetByIdAsync(
            productId,
            userId,
            includePublic: true,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure<FavoriteProductModel>(Errors.Product.NotFound(command.ProductId));
        }

        var existing = await favoriteProductRepository.GetByProductIdAsync(productId, userId, cancellationToken);
        if (existing is not null) {
            return Result.Failure<FavoriteProductModel>(Errors.FavoriteProduct.AlreadyExists);
        }

        var favorite = FavoriteProduct.Create(userId, productId, command.Name);
        await favoriteProductRepository.AddAsync(favorite, cancellationToken);

        var saved = await favoriteProductRepository.GetByIdAsync(favorite.Id, userId, cancellationToken: cancellationToken);
        return Result.Success(saved!.ToModel());
    }
}
