using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Common;

public interface IProductSearchSuggestionProvider {
    string Source { get; }

    Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken);
}
