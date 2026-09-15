using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.WeeklyCheckIn.Application.Models;

namespace FoodDiary.Modules.WeeklyCheckIn.Application.Queries.GetWeeklyCheckIn;

public record GetWeeklyCheckInQuery(
    Guid? UserId,
    DateOnly? WeekStart = null) : IQuery<Result<WeeklyCheckInModel>>, IUserRequest;
