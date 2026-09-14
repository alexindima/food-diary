using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Queries.ListFoodRecognitions;

public sealed record ListFoodRecognitionsQuery(Guid UserId) : IQuery<Result<IReadOnlyList<FoodRecognitionJobModel>>>;
