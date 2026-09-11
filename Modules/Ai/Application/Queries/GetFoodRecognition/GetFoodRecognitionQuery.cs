using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Queries.GetFoodRecognition;

public sealed record GetFoodRecognitionQuery(Guid UserId, Guid Id) : IQuery<Result<FoodRecognitionJobModel>>;
