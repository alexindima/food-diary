using FoodDiary.Modules.Meals.Domain.Entities;

namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

public interface IMealWriteRepository {
    Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default);

    Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default);

    Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default);
}
