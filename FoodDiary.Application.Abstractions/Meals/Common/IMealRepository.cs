namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealRepository :
    IMealReadRepository,
    IMealConsumptionReadRepository,
    IMealActivityReadRepository,
    IMealProductNutritionReadRepository,
    IMealWriteRepository;
