namespace FoodDiary.Modules.Recipes.Application.Abstractions.Common;

public interface IRecipeCatalogIdReadService {
    Task<bool?> CatalogIdIsPublicAsync(Guid id, CancellationToken cancellationToken = default);
}
