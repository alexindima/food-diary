using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Contracts.Models;

namespace FoodDiary.Modules.Products.Contracts.Common;

public interface IProductSnapshotReadService {
    Task<IReadOnlyDictionary<ProductId, ProductSnapshotReadModel>> GetByIdsAsync(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);
}
