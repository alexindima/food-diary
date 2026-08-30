using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.WeeklyCheckIn.Models;

namespace FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;

public record GetWeeklyCheckInQuery(
    Guid? UserId,
    DateOnly? WeekStart = null) : IQuery<Result<WeeklyCheckInModel>>, IUserRequest;
