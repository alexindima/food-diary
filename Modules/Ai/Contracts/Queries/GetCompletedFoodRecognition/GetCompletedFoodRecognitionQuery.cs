using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Contracts.Queries.GetCompletedFoodRecognition;

public sealed record GetCompletedFoodRecognitionQuery(
    Guid UserId,
    Guid JobId) : IRequest<Result<FoodRecognitionJobModel>>;
