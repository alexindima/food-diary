using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Modules.Ai.Application.Queries.ListFoodRecognitions;

public sealed record ListFoodRecognitionsQuery(Guid UserId, int Page = 1, int Limit = 20, bool? IsProductLabel = null) : IQuery<Result<PagedResponse<FoodRecognitionJobModel>>>;
