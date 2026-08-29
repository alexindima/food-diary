using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingOverview;

public sealed record GetFastingOverviewQuery(Guid? UserId) : IQuery<Result<FastingOverviewModel>>, IUserRequest;
