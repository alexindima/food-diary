using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealWriteRepository {
    Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default);

    Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default);

    Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default);
}
