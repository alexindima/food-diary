using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Queries.ListFoodRecognitions;

public sealed record ListFoodRecognitionsQuery(Guid UserId) : IQuery<Result<IReadOnlyList<FoodRecognitionJobModel>>>;
