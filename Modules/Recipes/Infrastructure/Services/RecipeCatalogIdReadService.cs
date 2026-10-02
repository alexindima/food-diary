using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Recipes.Infrastructure.Services;

internal sealed class RecipeCatalogIdReadService(RecipesDbContext context, Func<CancellationToken, Task> synchronizeTransactionAsync)
    : IRecipeCatalogIdReadService {
    public async Task<bool?> CatalogIdIsPublicAsync(Guid id, CancellationToken cancellationToken = default) {
        await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await context.Recipes.AsNoTracking().Where(item => item.Id == new RecipeId(id))
            .Select(item => (bool?)(item.Visibility == Visibility.Public)).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }
}
