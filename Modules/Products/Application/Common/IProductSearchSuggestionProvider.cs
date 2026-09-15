using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Common;

public interface IProductSearchSuggestionProvider {
    string Source { get; }

    Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken);
}
