using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetProfileOverview;

public sealed record GetProfileOverviewQuery(Guid? UserId) : IQuery<Result<ProfileOverviewModel>>, IUserRequest;
