using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.WeeklyCheckIn.Application.Models;

namespace FoodDiary.Modules.WeeklyCheckIn.Application.Queries.GetWeeklyCheckIn;

public record GetWeeklyCheckInQuery(
    Guid? UserId,
    DateOnly? WeekStart = null, string? TimeZoneId = null) : IQuery<Result<WeeklyCheckInModel>>, IUserRequest;
