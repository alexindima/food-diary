using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Wearables.Models;

namespace FoodDiary.Application.Wearables.Wearables.Queries.GetWearableDailySummary;

public record GetWearableDailySummaryQuery(Guid? UserId, DateTime Date)
    : IQuery<Result<WearableDailySummaryModel>>, IUserRequest;
