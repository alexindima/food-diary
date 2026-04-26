using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Wearables.Models;

namespace FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;

public record GetWearableDailySummaryQuery(Guid? UserId, DateTime Date)
    : IQuery<Result<WearableDailySummaryModel>>, IUserRequest;
