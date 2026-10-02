namespace FoodDiary.Modules.Products.Application.Abstractions.Common;

public interface IProductCatalogIdReadService {
    Task<bool?> CatalogIdIsPublicAsync(Guid id, CancellationToken cancellationToken = default);
}
