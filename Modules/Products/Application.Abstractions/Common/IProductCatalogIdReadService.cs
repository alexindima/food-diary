namespace FoodDiary.Modules.Products.Application.Abstractions.Common;

public interface IProductCatalogIdReadService {
    Task<bool?> CatalogIdIsPublicAsync(Guid id, CancellationToken cancellationToken = default);

    // True means a public FDC match; false means an ID collision; null means a new record.
    Task<bool?> FindUsdaCatalogMatchAsync(Guid id, int fdcId, CancellationToken cancellationToken = default);
}
