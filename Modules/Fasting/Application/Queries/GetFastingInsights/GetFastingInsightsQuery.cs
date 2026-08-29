using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingInsights;

public sealed record GetFastingInsightsQuery(Guid? UserId) : IQuery<Result<FastingInsightsModel>>, IUserRequest;
