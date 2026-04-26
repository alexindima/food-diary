using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingInsights;

public sealed record GetFastingInsightsQuery(Guid? UserId) : IQuery<Result<FastingInsightsModel>>, IUserRequest;
