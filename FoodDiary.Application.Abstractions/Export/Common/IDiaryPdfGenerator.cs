using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Export.Common;

public interface IDiaryPdfGenerator {
    byte[] Generate(IReadOnlyList<Meal> meals, DateTime dateFrom, DateTime dateTo);
}
