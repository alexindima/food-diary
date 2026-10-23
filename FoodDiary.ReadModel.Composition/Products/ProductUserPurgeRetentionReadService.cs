using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Products;

public sealed class ProductUserPurgeRetentionReadService(ICompositionReadContext context) : IProductUserPurgeRetentionReadService {
    public async Task<IReadOnlyCollection<ProductId>> GetRetainedProductIdsAsync(UserId sourceUserId, CancellationToken cancellationToken) =>
        await context.Products.AsNoTracking()
            .Where(product => product.UserId == sourceUserId)
            .Where(product =>
                context.RecipeIngredients.AsNoTracking().Any(ingredient => ingredient.ProductId == product.Id && ingredient.RecipeStep.Recipe.UserId != sourceUserId) ||
                context.MealItems.AsNoTracking().Any(item => item.ProductId == product.Id && item.Meal.UserId != sourceUserId) ||
                context.FavoriteProducts.AsNoTracking().Any(favorite => favorite.ProductId == product.Id && favorite.UserId != sourceUserId) ||
                context.ShoppingLists.AsNoTracking().Where(list => list.UserId != sourceUserId).SelectMany(list => list.Items)
                    .Any(item => item.ProductId == product.Id))
            .Select(product => product.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
}
