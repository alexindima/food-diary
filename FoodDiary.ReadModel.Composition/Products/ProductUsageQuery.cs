using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Products;

public sealed class ProductUsageQuery(ICompositionReadContext context) : IProductUsageQuery {
    public async Task<int> GetUsageCountAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        await context.Products
            .AsNoTracking()
            .Where(p => p.Id == id && (includePublic
                ? p.UserId == userId || p.Visibility == Visibility.Public
                : p.UserId == userId))
            .Select(p => context.MealItems.AsNoTracking().Count(item => item.ProductId == p.Id) + context.RecipeIngredients.AsNoTracking().Count(ingredient => ingredient.ProductId == p.Id))
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

}
