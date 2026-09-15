namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

public interface IMealRepository :
    IMealReadRepository,
    IMealProjectionReadRepository,
    IMealActivityReadRepository,
    IMealProductNutritionReadRepository,
    IMealWriteRepository;
