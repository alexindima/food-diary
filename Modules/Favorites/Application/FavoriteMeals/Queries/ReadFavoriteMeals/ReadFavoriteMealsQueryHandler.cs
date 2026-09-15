using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Mappings;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadFavoriteMeals;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadFavoriteMeals;

public sealed class ReadFavoriteMealsQueryHandler(IFavoriteMealReadModelRepository favoriteMealReadModelRepository) : IQueryHandler<ReadFavoriteMealsQuery, IReadOnlyList<FavoriteMealModel>> {
    public async Task<IReadOnlyList<FavoriteMealModel>> Handle(ReadFavoriteMealsQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        IReadOnlyList<FavoriteMealReadModel> favorites = await favoriteMealReadModelRepository.GetAllReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(favorite => favorite.ToModel())];
    }

}
