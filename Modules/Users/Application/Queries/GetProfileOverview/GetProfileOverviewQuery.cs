using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Users.Application.Queries.GetProfileOverview;

public sealed record GetProfileOverviewQuery(Guid? UserId) : IQuery<Result<ProfileOverviewModel>>, IUserRequest;
