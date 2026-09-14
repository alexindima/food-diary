using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Queries.GetFoodRecognition;

public sealed record GetFoodRecognitionQuery(Guid UserId, Guid Id) : IQuery<Result<FoodRecognitionJobModel>>;
