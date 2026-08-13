namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealRepository :
    IMealReadRepository,
    IMealProjectionReadRepository,
    IMealActivityReadRepository,
    IMealProductNutritionReadRepository,
    IMealWriteRepository;
