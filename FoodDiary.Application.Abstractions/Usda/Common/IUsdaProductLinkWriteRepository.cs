using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public interface IUsdaProductLinkWriteRepository : IUsdaProductLinkReadRepository {
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}
