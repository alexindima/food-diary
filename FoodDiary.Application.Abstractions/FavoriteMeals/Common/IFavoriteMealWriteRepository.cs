using FoodDiary.Domain.Entities.FavoriteMeals;

namespace FoodDiary.Application.Abstractions.FavoriteMeals.Common;

public interface IFavoriteMealWriteRepository : IFavoriteMealReadRepository {
    Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default);
}
