using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Products.Common;

public interface IProductSnapshotReadService {
    Task<IReadOnlyDictionary<ProductId, ProductSnapshotReadModel>> GetByIdsAsync(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);
}
