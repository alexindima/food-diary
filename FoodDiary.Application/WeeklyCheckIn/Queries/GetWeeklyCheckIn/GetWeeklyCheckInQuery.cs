using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.WeeklyCheckIn.Models;

namespace FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;

public record GetWeeklyCheckInQuery(
    Guid? UserId) : IQuery<Result<WeeklyCheckInModel>>, IUserRequest;
