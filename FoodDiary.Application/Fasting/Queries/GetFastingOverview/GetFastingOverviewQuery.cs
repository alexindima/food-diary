using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingOverview;

public sealed record GetFastingOverviewQuery(Guid? UserId) : IQuery<Result<FastingOverviewModel>>, IUserRequest;
