using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingOverview;

public sealed record GetFastingOverviewQuery(Guid? UserId) : IQuery<Result<FastingOverviewModel>>, IUserRequest;
