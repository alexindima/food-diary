using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;

namespace FoodDiary.Modules.Wearables.Application.Queries.GetWearableDailySummary;

public record GetWearableDailySummaryQuery(Guid? UserId, DateTime Date)
    : IQuery<Result<WearableDailySummaryModel>>, IUserRequest;
