using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Wearables.Models;

namespace FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;

public record GetWearableDailySummaryQuery(Guid? UserId, DateTime Date)
    : IQuery<Result<WearableDailySummaryModel>>, IUserRequest;
