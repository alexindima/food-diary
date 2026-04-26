using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetProfileOverview;

public sealed record GetProfileOverviewQuery(Guid? UserId) : IQuery<Result<ProfileOverviewModel>>, IUserRequest;
