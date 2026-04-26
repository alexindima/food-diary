using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.WeeklyCheckIn.Models;

namespace FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;

public record GetWeeklyCheckInQuery(
    Guid? UserId) : IQuery<Result<WeeklyCheckInModel>>, IUserRequest;
