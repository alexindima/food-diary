using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IDailyAdviceRepository
{
    Task<IReadOnlyList<DailyAdvice>> GetByLocaleAsync(
        string locale,
        CancellationToken cancellationToken = default);
}


